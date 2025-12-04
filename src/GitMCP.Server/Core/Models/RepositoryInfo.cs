namespace GitMCP.Server.Core.Models;

/// <summary>
/// 仓库信息模型
/// </summary>
public class RepositoryInfo
{
    /// <summary>
    /// 仓库名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 本地路径
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// 远程仓库 URL
    /// </summary>
    public string? RemoteUrl { get; set; }

    /// <summary>
    /// 仓库描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// 当前分支
    /// </summary>
    public string? CurrentBranch { get; set; }
}
