using GitMCP.Server.Core.Interfaces;
using System.Collections.Concurrent;

namespace GitMCP.Server.Tools;

/// <summary>
/// 简单工具注册器
/// </summary>
public class SimpleToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, IMcpTool> _tools = new();

    public void RegisterTool(IMcpTool tool)
    {
        if (_tools.TryAdd(tool.Name, tool))
        {
            Console.WriteLine($"已注册工具: {tool.Name}");
        }
        else
        {
            Console.WriteLine($"工具已存在: {tool.Name}");
        }
    }

    public IMcpTool? GetTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    public IEnumerable<IMcpTool> GetAllTools()
    {
        return _tools.Values;
    }

    public async Task<object> ExecuteToolAsync(string name, Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var tool = GetTool(name);
        if (tool == null)
        {
            throw new InvalidOperationException($"工具未找到: {name}");
        }

        return await tool.ExecuteAsync(args, cancellationToken);
    }
}
