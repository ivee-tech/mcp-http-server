using System.Text.Json.Serialization;

namespace McpHttpServer.Mcp;

/// <summary>
/// Response payload for listing tools.
/// </summary>
public sealed record ToolListResponse(
    [property: JsonPropertyName("tools")] IReadOnlyList<ToolDefinition> Tools);
