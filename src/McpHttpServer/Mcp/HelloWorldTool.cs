using System.Text.Json.Nodes;

namespace McpHttpServer.Mcp;

/// <summary>
/// Simple tool that returns a greeting.
/// </summary>
public sealed class HelloWorldTool : IMcpTool
{
    public ToolDefinition Definition { get; } = new(
        Name: "hello_world",
        Description: "Returns a friendly greeting. Provide an optional 'name' field in the input.",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["title"] = "HelloWorldInput",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["name"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Optional name to include in the greeting."
                }
            }
        });

    public ValueTask<ToolExecutionResult> ExecuteAsync(JsonObject? input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? name = null;
        if (input is not null && input.TryGetPropertyValue("name", out var nameNode) && nameNode is JsonValue nameValue)
        {
            name = nameValue.GetValue<string?>();
        }

        name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var message = string.IsNullOrWhiteSpace(name) ? "Hello, world!" : $"Hello, {name}!";

        var payload = new JsonObject
        {
            ["message"] = message
        };

        return ValueTask.FromResult(new ToolExecutionResult("application/json", payload));
    }
}
