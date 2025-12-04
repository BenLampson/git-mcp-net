using GitMCP.Server.Core.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitMCP.Server.GitHub;

/// <summary>
/// Git 仓库扫描器
/// </summary>
public class GitRepositoryScanner
{
    private readonly ILogger<GitRepositoryScanner> _logger;

    public GitRepositoryScanner(ILogger<GitRepositoryScanner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 扫描本地仓库目录
    /// </summary>
    /// <param name="baseDir">基础目录</param>
    /// <returns>仓库信息列表</returns>
    public List<RepositoryInfo> ScanLocalRepositories(string baseDir)
    {
        var repos = new List<RepositoryInfo>();

        if (!Directory.Exists(baseDir))
        {
            _logger.LogWarning("仓库目录不存在: {BaseDir}", baseDir);
            return repos;
        }

        _logger.LogInformation("开始扫描仓库目录: {BaseDir}", baseDir);

        foreach (var dir in Directory.GetDirectories(baseDir))
        {
            var gitDir = Path.Combine(dir, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                try
                {
                    var repoInfo = ExtractRepositoryInfo(dir);
                    repos.Add(repoInfo);
                    _logger.LogInformation("发现仓库: {Name} ({Path})", repoInfo.Name, repoInfo.LocalPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "无法解析仓库: {Dir}", dir);
                }
            }
        }

        _logger.LogInformation("扫描完成，共发现 {Count} 个仓库", repos.Count);
        return repos;
    }

    /// <summary>
    /// 提取仓库信息
    /// </summary>
    private RepositoryInfo ExtractRepositoryInfo(string repoPath)
    {
        using var repo = new Repository(repoPath);

        var repoInfo = new RepositoryInfo
        {
            Name = Path.GetFileName(repoPath),
            LocalPath = repoPath,
            CurrentBranch = repo.Head.FriendlyName
        };

        // 获取远程 URL
        var origin = repo.Network.Remotes["origin"];
        if (origin != null)
        {
            repoInfo.RemoteUrl = origin.Url;
        }

        // 获取最后提交时间
        var lastCommit = repo.Head.Tip;
        if (lastCommit != null)
        {
            repoInfo.LastUpdated = lastCommit.Author.When.DateTime;
        }

        return repoInfo;
    }
}
