using GitMCP.Server.Core.Interfaces;
using GitMCP.Server.Core.Models;
using System.Text;
using System.Text.Json;

namespace GitMCP.Server.Tools;

/// <summary>
/// 搜索文档工具
/// </summary>
public class SearchDocumentationTool : IMcpTool
{
    private readonly RepositoryInfo _repository;

    public SearchDocumentationTool(RepositoryInfo repository)
    {
        _repository = repository;
    }

    public string Name => $"search_{SanitizeName(_repository.Name)}_docs";

    public string Description => $"Semantically search within the fetched documentation from repository: {_repository.Name}. Useful for specific queries.";

    public object InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "The search query to find relevant documentation"
                }
            },
            "required": ["query"]
        }
        """);

    public async Task<object> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.TryGetValue("query", out var queryObj))
        {
            throw new ArgumentException("Missing required argument 'query'");
        }

        var query = queryObj.ToString() ?? "";
        
        // 搜索文档文件
        var docFiles = FindDocumentationFiles(_repository.LocalPath);
        var results = new List<object>();

        foreach (var file in docFiles)
        {
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                var snippet = ExtractSnippet(content, query);
                results.Add(new
                {
                    file = Path.GetRelativePath(_repository.LocalPath, file),
                    snippet = snippet,
                    repository = _repository.Name
                });

                if (results.Count >= 5) // 限制结果数量
                    break;
            }
        }

        return new
        {
            query = query,
            repository = _repository.Name,
            results = results,
            total = results.Count
        };
    }

    private List<string> FindDocumentationFiles(string repoPath)
    {
        var docFiles = new List<string>();
        var patterns = new[] { "*.md", "*.txt", "*.rst" };
        var commonDocDirs = new[] { "docs", "doc", "documentation", "." };

        foreach (var dir in commonDocDirs)
        {
            var fullPath = Path.Combine(repoPath, dir);
            if (!Directory.Exists(fullPath))
                continue;

            foreach (var pattern in patterns)
            {
                try
                {
                    docFiles.AddRange(Directory.GetFiles(fullPath, pattern, SearchOption.AllDirectories));
                }
                catch
                {
                    // 忽略权限错误
                }
            }
        }

        return docFiles;
    }

    private string ExtractSnippet(string content, string query, int contextLength = 200)
    {
        var index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            return content.Length > contextLength ? content.Substring(0, contextLength) + "..." : content;

        var start = Math.Max(0, index - contextLength / 2);
        var length = Math.Min(content.Length - start, contextLength);
        var snippet = content.Substring(start, length);

        if (start > 0)
            snippet = "..." + snippet;
        if (start + length < content.Length)
            snippet += "...";

        return snippet;
    }

    private string SanitizeName(string name)
    {
        return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray())
            .ToLowerInvariant();
    }
}
