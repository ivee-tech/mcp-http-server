using System.Text.Json;
using System.Text.Json.Nodes;
using McpHttpServer.Mcp;

var builder = WebApplication.CreateBuilder(args);

var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = jsonSerializerOptions.PropertyNamingPolicy;
    options.SerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
});

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IMcpTool, HelloWorldTool>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "mcp-http-server",
    status = "ok",
    endpoints = new[] { "/tools", "/execute", "/mcp/tools", "/mcp/execute" },
    capabilities = new { jsonrpc = true }
}));

app.MapPost("/", HandleJsonRpcAsync);

MapToolsEndpoint(app, "/tools");
MapToolsEndpoint(app, "/mcp/tools");

MapExecuteEndpoint(app, "/execute");
MapExecuteEndpoint(app, "/mcp/execute");

app.Run();

RouteHandlerBuilder MapToolsEndpoint(IEndpointRouteBuilder routes, string pattern) =>
    routes.MapGet(pattern, (IToolRegistry registry) => Results.Ok(new ToolListResponse(registry.List())))
        .Produces<ToolListResponse>(StatusCodes.Status200OK);

RouteHandlerBuilder MapExecuteEndpoint(IEndpointRouteBuilder routes, string pattern) =>
    routes.MapPost(pattern, ExecuteToolAsync)
        .Produces<ToolExecutionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

async Task<IResult> ExecuteToolAsync(
    ToolExecutionRequest request,
    IToolRegistry registry,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.Tool))
    {
        return Results.Problem(
            title: "Invalid request",
            detail: "The 'tool' field is required.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (!registry.TryGetTool(request.Tool, out var tool) || tool is null)
    {
        return Results.Problem(
            title: "Tool not found",
            detail: $"The tool '{request.Tool}' is not registered.",
            statusCode: StatusCodes.Status404NotFound);
    }

    ToolExecutionResult result;
    try
    {
        result = await tool.ExecuteAsync(request.Input, cancellationToken);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while executing tool {ToolName}", tool.Definition.Name);
        return Results.Problem(
            title: "Execution failed",
            detail: "The tool encountered an unexpected error.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    return Results.Stream(async stream =>
    {
        var response = new ToolExecutionResponse(tool.Definition.Name, result.ContentType, result.Payload);
        await JsonSerializer.SerializeAsync(stream, response, jsonSerializerOptions, cancellationToken);
    }, "application/json");
}

async Task<IResult> HandleJsonRpcAsync(
    HttpContext context,
    IToolRegistry registry,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    JsonNode? payload;
    try
    {
        payload = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: cancellationToken);
    }
    catch (JsonException)
    {
        return Results.Json(CreateErrorResponse(null, -32700, "Parse error"));
    }

    if (payload is not JsonObject request)
    {
        return Results.Json(CreateErrorResponse(null, -32600, "Invalid request"));
    }

    request.TryGetPropertyValue("id", out var idNode);

    if (!request.TryGetPropertyValue("method", out var methodNode) || methodNode?.GetValue<string>() is not { } method)
    {
        return Results.Json(CreateErrorResponse(idNode, -32600, "Missing method"));
    }

    switch (method)
    {
        case "initialize":
            return Results.Json(CreateSuccessResponse(idNode, BuildInitializeResult()));
        case "ping":
            return Results.Json(CreateSuccessResponse(idNode, new JsonObject()));
        case "tools/list":
            return Results.Json(CreateSuccessResponse(idNode, BuildToolListResult(registry)));
        case "tools/call":
            return await HandleToolCallAsync(request, idNode, registry, logger, cancellationToken);
        default:
            return Results.Json(CreateErrorResponse(idNode, -32601, $"Method '{method}' not found"));
    }
}

JsonObject BuildInitializeResult() => new()
{
    ["protocolVersion"] = "2024-11-05",
    ["serverInfo"] = new JsonObject
    {
        ["name"] = "mcp-http-server",
        ["version"] = "0.1.0"
    },
    ["capabilities"] = new JsonObject
    {
        ["tools"] = new JsonObject
        {
            ["list"] = new JsonObject(),
            ["call"] = new JsonObject()
        }
    }
};

JsonObject BuildToolListResult(IToolRegistry registry)
{
    var toolsArray = new JsonArray();
    foreach (var tool in registry.List())
    {
        toolsArray.Add(tool.ToJson());
    }

    return new JsonObject
    {
        ["tools"] = toolsArray
    };
}

async Task<IResult> HandleToolCallAsync(
    JsonObject request,
    JsonNode? idNode,
    IToolRegistry registry,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    request.TryGetPropertyValue("params", out var paramsNode);
    var paramsObject = paramsNode as JsonObject;

    var toolName = paramsObject? ["name"]?.GetValue<string?>()
        ?? paramsObject? ["tool"]?.GetValue<string?>();

    if (string.IsNullOrWhiteSpace(toolName))
    {
        return Results.Json(CreateErrorResponse(idNode, -32602, "Tool name is required."));
    }

    if (!registry.TryGetTool(toolName!, out var tool) || tool is null)
    {
        return Results.Json(CreateErrorResponse(idNode, -32602, $"Tool '{toolName}' not found."));
    }

    var argumentsNode = paramsObject? ["arguments"] as JsonObject
        ?? paramsObject? ["input"] as JsonObject;

    try
    {
        var result = await tool.ExecuteAsync(argumentsNode, cancellationToken);
        var contentItems = CreateContentItems(result);

        var response = new JsonObject
        {
            ["tool"] = tool.Definition.Name,
            ["content"] = contentItems
        };

        return Results.Json(CreateSuccessResponse(idNode, response));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while executing tool {ToolName}", tool.Definition.Name);
        return Results.Json(CreateErrorResponse(idNode, -32603, "Tool execution failed."));
    }
}

JsonArray CreateContentItems(ToolExecutionResult result)
{
    var content = new JsonArray();

    if (string.Equals(result.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
    {
        content.Add(new JsonObject
        {
            ["type"] = "json",
            ["json"] = JsonNode.Parse(result.Payload.ToJsonString())
        });
    }
    else if (string.Equals(result.ContentType, "text/plain", StringComparison.OrdinalIgnoreCase))
    {
        var text = result.Payload switch
        {
            JsonValue value => value.TryGetValue<string>(out var str) ? str : value.ToString(),
            _ => result.Payload.ToJsonString()
        };

        content.Add(new JsonObject
        {
            ["type"] = "text",
            ["text"] = text ?? string.Empty
        });
    }
    else
    {
        content.Add(new JsonObject
        {
            ["type"] = "json",
            ["json"] = new JsonObject
            {
                ["contentType"] = result.ContentType,
                ["payload"] = JsonNode.Parse(result.Payload.ToJsonString())
            }
        });
    }

    return content;
}

JsonObject CreateSuccessResponse(JsonNode? idNode, JsonObject result) => new()
{
    ["jsonrpc"] = "2.0",
    ["id"] = idNode?.DeepClone() ?? JsonValue.Create((string?)null),
    ["result"] = result
};

JsonObject CreateErrorResponse(JsonNode? idNode, int code, string message) => new()
{
    ["jsonrpc"] = "2.0",
    ["id"] = idNode?.DeepClone() ?? JsonValue.Create((string?)null),
    ["error"] = new JsonObject
    {
        ["code"] = code,
        ["message"] = message
    }
};

public partial class Program;
