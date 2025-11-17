using System.Text.Json.Nodes;

namespace McpHttpServer.Mcp;

/// <summary>
/// Describes a tool that can be executed by the MCP server.
/// </summary>
public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonObject InputSchema);
