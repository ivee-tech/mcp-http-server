using System.Collections.Concurrent;

namespace McpHttpServer.Mcp;

/// <summary>
/// Default registry that keeps track of available tools.
/// </summary>
public interface IToolRegistry
{
    IReadOnlyList<ToolDefinition> List();
    bool TryGetTool(string name, out IMcpTool? tool);
}

public sealed class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, IMcpTool> _tools;

    public ToolRegistry(IEnumerable<IMcpTool> tools)
    {
        _tools = new ConcurrentDictionary<string, IMcpTool>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tools)
        {
            _tools.AddOrUpdate(tool.Definition.Name, tool, (_, _) => tool);
        }
    }

    public IReadOnlyList<ToolDefinition> List() =>
        _tools.Values.Select(tool => tool.Definition).OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool TryGetTool(string name, out IMcpTool? tool)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            tool = null;
            return false;
        }

        return _tools.TryGetValue(name, out tool);
    }
}
