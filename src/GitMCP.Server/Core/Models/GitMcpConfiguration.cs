namespace GitMCP.Server.Core.Models;

/// <summary>
/// GitMCP 配置
/// </summary>
public class GitMcpConfiguration
{
    /// <summary>
    /// 仓库基础目录
    /// </summary>
    public string RepositoryBaseDirectory { get; set; } = "D:/GitRepos";

    /// <summary>
    /// 启动时是否 Pull
    /// </summary>
    public bool PullOnStartup { get; set; } = true;

    /// <summary>
    /// Pull 超时时间（秒）
    /// </summary>
    public int PullTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 最大并发 Pull 数
    /// </summary>
    public int MaxConcurrentPulls { get; set; } = 3;

    /// <summary>
    /// 服务器信息
    /// </summary>
    public ServerInfo ServerInfo { get; set; } = new();
}

public class ServerInfo
{
    public string Name { get; set; } = "GitMCP.Server";
    public string Version { get; set; } = "1.0.0";
}
