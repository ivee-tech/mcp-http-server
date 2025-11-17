using System.Text.Json.Nodes;

namespace McpHttpServer.Mcp;

/// <summary>
/// MCP tool abstraction.
/// </summary>
public interface IMcpTool
{
    ToolDefinition Definition { get; }

    ValueTask<ToolExecutionResult> ExecuteAsync(JsonObject? input, CancellationToken cancellationToken);
}
