using GitMCP.Server.Core.Interfaces;
using GitMCP.Server.Core.Models;

namespace GitMCP.Server.Tools;

/// <summary>
/// 默认工具提供者
/// </summary>
public class DefaultToolProvider
{
    /// <summary>
    /// 为仓库创建默认工具
    /// </summary>
    /// <param name="repository">仓库信息</param>
    /// <returns>工具集合</returns>
    public IEnumerable<IMcpTool> CreateToolsForRepository(RepositoryInfo repository)
    {
        yield return new FetchDocumentationTool(repository);
        yield return new SearchDocumentationTool(repository);
        yield return new SearchCodeTool(repository);
    }

    /// <summary>
    /// 创建全局通用工具
    /// </summary>
    /// <returns>全局工具集合</returns>
    public IEnumerable<IMcpTool> CreateGlobalTools()
    {
        yield return new FetchUrlContentTool();
    }
}
