using System.Text.Json.Nodes;

namespace McpHttpServer.Mcp;

/// <summary>
/// Represents the data returned by a tool execution.
/// </summary>
public sealed record ToolExecutionResult(
    string ContentType,
    JsonNode Payload);
