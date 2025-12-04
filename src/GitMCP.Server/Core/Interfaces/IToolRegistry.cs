namespace GitMCP.Server.Core.Interfaces;

/// <summary>
/// 工具注册器接口
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// 注册工具
    /// </summary>
    /// <param name="tool">工具实例</param>
    void RegisterTool(IMcpTool tool);

    /// <summary>
    /// 获取工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>工具实例，如果不存在则返回 null</returns>
    IMcpTool? GetTool(string name);

    /// <summary>
    /// 获取所有工具
    /// </summary>
    /// <returns>所有已注册的工具</returns>
    IEnumerable<IMcpTool> GetAllTools();

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<object> ExecuteToolAsync(string name, Dictionary<string, object> args, CancellationToken cancellationToken = default);
}
