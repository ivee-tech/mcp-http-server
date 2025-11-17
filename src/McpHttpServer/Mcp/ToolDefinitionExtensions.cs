using System.Text.Json.Nodes;

namespace McpHttpServer.Mcp;

/// <summary>
/// Helper extensions for converting tool definitions to wire representations.
/// </summary>
public static class ToolDefinitionExtensions
{
    public static JsonObject ToJson(this ToolDefinition definition)
    {
        var json = new JsonObject
        {
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["inputSchema"] = definition.InputSchema?.DeepClone() ?? new JsonObject()
        };

        return json;
    }
}
