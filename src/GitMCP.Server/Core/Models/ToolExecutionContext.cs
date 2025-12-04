namespace GitMCP.Server.Core.Models;

/// <summary>
/// 工具执行上下文
/// </summary>
public class ToolExecutionContext
{
    /// <summary>
    /// 仓库信息
    /// </summary>
    public RepositoryInfo Repository { get; set; } = new();

    /// <summary>
    /// 执行参数
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
