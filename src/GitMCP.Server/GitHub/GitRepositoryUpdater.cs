using GitMCP.Server.Core.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitMCP.Server.GitHub;

/// <summary>
/// Git 仓库更新器
/// </summary>
public class GitRepositoryUpdater
{
    private readonly ILogger<GitRepositoryUpdater> _logger;

    public GitRepositoryUpdater(ILogger<GitRepositoryUpdater> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Pull 最新代码
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>是否成功</returns>
    public async Task<bool> PullLatestAsync(string repoPath, int timeoutSeconds = 300)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(repoPath);

                _logger.LogInformation("开始 pull 仓库: {Path}", repoPath);

                // 检查是否有远程仓库
                var remote = repo.Network.Remotes["origin"];
                if (remote == null)
                {
                    _logger.LogWarning("仓库没有 origin 远程: {Path}", repoPath);
                    return false;
                }

                // Fetch
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                var fetchOptions = new FetchOptions
                {
                    CredentialsProvider = (url, usernameFromUrl, types) => new DefaultCredentials()
                };

                Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, "Fetching updates");

                // Pull
                var signature = new Signature("GitMCP Bot", "bot@gitmcp.local", DateTimeOffset.Now);
                var pullOptions = new PullOptions
                {
                    FetchOptions = fetchOptions
                };

                var result = Commands.Pull(repo, signature, pullOptions);

                switch (result.Status)
                {
                    case MergeStatus.UpToDate:
                        _logger.LogInformation("仓库已是最新: {Path}", repoPath);
                        break;
                    case MergeStatus.FastForward:
                        _logger.LogInformation("仓库已更新 (Fast-forward): {Path}", repoPath);
                        break;
                    case MergeStatus.NonFastForward:
                        _logger.LogInformation("仓库已更新 (Non-fast-forward): {Path}", repoPath);
                        break;
                    default:
                        _logger.LogWarning("Pull 状态: {Status}, 仓库: {Path}", result.Status, repoPath);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pull 失败: {Path}", repoPath);
                return false;
            }
        });
    }

    /// <summary>
    /// 批量更新仓库
    /// </summary>
    /// <param name="repositories">仓库列表</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <returns>成功更新的仓库数量</returns>
    public async Task<int> PullMultipleAsync(IEnumerable<RepositoryInfo> repositories, int maxConcurrency = 3)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();

        foreach (var repo in repositories)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await PullLatestAsync(repo.LocalPath);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
}
