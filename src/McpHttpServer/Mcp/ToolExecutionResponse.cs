using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace McpHttpServer.Mcp;

/// <summary>
/// Envelope returned to clients after a tool has been executed.
/// </summary>
public sealed record ToolExecutionResponse(
    [property: JsonPropertyName("tool")] string Tool,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("result")] JsonNode Result);
