using GitMCP.Server.Core.Interfaces;
using GitMCP.Server.Core.Models;
using System.Text.Json;

namespace GitMCP.Server.Tools;

/// <summary>
/// 获取主文档工具
/// </summary>
public class FetchDocumentationTool : IMcpTool
{
    private readonly RepositoryInfo _repository;

    public FetchDocumentationTool(RepositoryInfo repository)
    {
        _repository = repository;
    }

    public string Name => $"fetch_{SanitizeName(_repository.Name)}_docs";

    public string Description => $"Fetch entire documentation file from repository: {_repository.Name}. Useful for general questions. Always call this tool first if asked about {_repository.Name}.";

    public object InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "section": {
                    "type": "string",
                    "description": "Optional section name to fetch specific documentation part"
                }
            }
        }
        """);

    public async Task<object> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        string? section = null;
        if (args.TryGetValue("section", out var sectionObj))
        {
            section = sectionObj.ToString();
        }

        // 按优先级查找文档文件
        var docFile = FindMainDocumentation(_repository.LocalPath);
        
        if (docFile == null)
        {
            return new
            {
                repository = _repository.Name,
                error = "No main documentation found",
                message = "Could not find llms.txt, README.md, or docs/index.md"
            };
        }

        var content = await File.ReadAllTextAsync(docFile, cancellationToken);
        var relativePath = Path.GetRelativePath(_repository.LocalPath, docFile);

        // 如果指定了 section，尝试提取该部分
        if (!string.IsNullOrEmpty(section))
        {
            content = ExtractSection(content, section);
        }

        return new
        {
            repository = _repository.Name,
            file = relativePath,
            section = section,
            content = content,
            size = content.Length,
            lastModified = File.GetLastWriteTimeUtc(docFile)
        };
    }

    private string? FindMainDocumentation(string repoPath)
    {
        // 优先级 1: llms.txt (AI 优化文档)
        var llmsTxt = Path.Combine(repoPath, "llms.txt");
        if (File.Exists(llmsTxt))
            return llmsTxt;

        // 优先级 2: README.md (根目录)
        var readme = Path.Combine(repoPath, "README.md");
        if (File.Exists(readme))
            return readme;

        // 优先级 3: docs/README.md
        var docsReadme = Path.Combine(repoPath, "docs", "README.md");
        if (File.Exists(docsReadme))
            return docsReadme;

        // 优先级 4: docs/index.md
        var docsIndex = Path.Combine(repoPath, "docs", "index.md");
        if (File.Exists(docsIndex))
            return docsIndex;

        // 优先级 5: README (无扩展名)
        var readmeNoExt = Path.Combine(repoPath, "README");
        if (File.Exists(readmeNoExt))
            return readmeNoExt;

        return null;
    }

    private string ExtractSection(string content, string sectionName)
    {
        var lines = content.Split('\n');
        var sectionLines = new List<string>();
        bool inSection = false;
        int sectionLevel = 0;

        foreach (var line in lines)
        {
            // 检测 Markdown 标题
            if (line.TrimStart().StartsWith('#'))
            {
                var currentLevel = line.TakeWhile(c => c == '#').Count();
                var title = line.TrimStart('#').Trim();

                if (title.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    sectionLevel = currentLevel;
                    sectionLines.Add(line);
                    continue;
                }

                // 如果遇到同级或更高级别的标题，停止
                if (inSection && currentLevel <= sectionLevel)
                {
                    break;
                }
            }

            if (inSection)
            {
                sectionLines.Add(line);
            }
        }

        return sectionLines.Count > 0 
            ? string.Join('\n', sectionLines) 
            : content; // 如果没找到 section，返回全部内容
    }

    private string SanitizeName(string name)
    {
        return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray())
            .ToLowerInvariant();
    }
}
