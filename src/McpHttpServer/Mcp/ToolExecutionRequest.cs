using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace McpHttpServer.Mcp;

/// <summary>
/// Represents a request to execute a tool.
/// </summary>
public sealed record ToolExecutionRequest(
    [property: Required]
    [property: JsonPropertyName("tool")]
    string Tool,
    [property: JsonPropertyName("input")]
    JsonObject? Input);
