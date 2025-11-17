using System.Net;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using McpHttpServer.Mcp;
using Microsoft.AspNetCore.Mvc.Testing;

namespace McpHttpServer.IntegrationTests;

public class McpEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public McpEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task ListTools_ReturnsHelloWorldTool()
    {
        var response = await _client.GetAsync("/mcp/tools");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ToolListResponse>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Tools, tool => string.Equals(tool.Name, "hello_world", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Execute_ReturnsGreeting()
    {
        var requestBody = new
        {
            tool = "hello_world",
            input = new
            {
                name = "Azure"
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp/execute", requestBody);

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<ToolExecutionResponse>();
        Assert.NotNull(payload);
        Assert.Equal("hello_world", payload!.Tool);
        Assert.Equal("application/json", payload.ContentType);
        var message = payload.Result?["message"]?.GetValue<string>();
        Assert.Equal("Hello, Azure!", message);
    }

    [Fact]
    public async Task Execute_WithUnknownTool_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/mcp/execute", new { tool = "missing" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task JsonRpcInitialize_ReturnsCapabilities()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new { }
        };

        var response = await _client.PostAsJsonAsync("/", requestBody);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(payload);
        Assert.Equal("2.0", payload!["jsonrpc"]?.GetValue<string>());
        Assert.NotNull(payload["result"]?["capabilities"]?["tools"]);
    }

    [Fact]
    public async Task JsonRpcToolsList_ReturnsToolMetadata()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        var response = await _client.PostAsJsonAsync("/", requestBody);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(payload);
        var tools = payload!["result"]?["tools"]?.AsArray();
        Assert.NotNull(tools);
        Assert.Contains(tools!, tool => tool?["name"]?.GetValue<string>() == "hello_world");
    }

    [Fact]
    public async Task JsonRpcToolsCall_ReturnsGreeting()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "hello_world",
                arguments = new
                {
                    name = "Azure"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/", requestBody);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(payload);
        var message = payload!["result"]?["content"]?
            .AsArray().FirstOrDefault()? ["json"]? ["message"]?.GetValue<string>();
        Assert.Equal("Hello, Azure!", message);
    }
}
