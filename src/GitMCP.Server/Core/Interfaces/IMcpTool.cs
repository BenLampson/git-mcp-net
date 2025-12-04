namespace GitMCP.Server.Core.Interfaces;

/// <summary>
/// MCP 工具接口
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 参数 Schema (JSON Schema)
    /// </summary>
    object InputSchema { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<object> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default);
}
