using GitMCP.Server.Core.Interfaces;
using GitMCP.Server.Core.Models;
using System.Text.Json;

namespace GitMCP.Server.Tools;

/// <summary>
/// 搜索代码工具
/// </summary>
public class SearchCodeTool : IMcpTool
{
    private readonly RepositoryInfo _repository;

    public SearchCodeTool(RepositoryInfo repository)
    {
        _repository = repository;
    }

    public string Name => $"search_{SanitizeName(_repository.Name)}_code";

    public string Description => $"Search for code within the repository: \"{_repository.Name}\" using semantic search. Returns matching code snippets and files for you to query further if relevant.";

    public object InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "The search query to find relevant code"
                },
                "fileExtension": {
                    "type": "string",
                    "description": "Optional file extension filter (e.g., 'cs', 'js', 'py')"
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
        string? fileExtension = null;

        if (args.TryGetValue("fileExtension", out var extObj))
        {
            fileExtension = extObj.ToString();
        }

        // 搜索代码文件
        var codeFiles = FindCodeFiles(_repository.LocalPath, fileExtension);
        var results = new List<object>();

        foreach (var file in codeFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    var snippet = ExtractCodeSnippet(content, query);
                    results.Add(new
                    {
                        file = Path.GetRelativePath(_repository.LocalPath, file),
                        snippet = snippet,
                        language = Path.GetExtension(file).TrimStart('.'),
                        repository = _repository.Name
                    });

                    if (results.Count >= 10) // 限制结果数量
                        break;
                }
            }
            catch
            {
                // 忽略无法读取的文件
            }
        }

        return new
        {
            query = query,
            repository = _repository.Name,
            fileExtension = fileExtension,
            results = results,
            total = results.Count
        };
    }

    private List<string> FindCodeFiles(string repoPath, string? fileExtension)
    {
        var codeFiles = new List<string>();
        var extensions = fileExtension != null
            ? new[] { $"*.{fileExtension.TrimStart('.')}" }
            : new[] { 
                "*.cs", "*.js", "*.ts", "*.jsx", "*.tsx",        // C#, JavaScript, TypeScript, React
                "*.vue", "*.svelte",                              // Vue, Svelte
                "*.py", "*.java", "*.cpp", "*.c", "*.h",         // Python, Java, C/C++
                "*.go", "*.rs", "*.rb", "*.php",                 // Go, Rust, Ruby, PHP
                "*.swift", "*.kt", "*.scala"                     // Swift, Kotlin, Scala
            };

        var excludeDirs = new[] { ".git", "node_modules", "bin", "obj", "dist", "build", ".vs" };

        try
        {
            foreach (var ext in extensions)
            {
                var files = Directory.GetFiles(repoPath, ext, SearchOption.AllDirectories)
                    .Where(f => !excludeDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar)))
                    .Take(100); // 限制文件数量避免扫描太多

                codeFiles.AddRange(files);
            }
        }
        catch
        {
            // 忽略权限错误
        }

        return codeFiles;
    }

    private string ExtractCodeSnippet(string content, string query, int maxLines = 20)
    {
        var lines = content.Split('\n');
        var queryIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                queryIndex = i;
                break;
            }
        }

        if (queryIndex == -1)
        {
            return string.Join('\n', lines.Take(maxLines));
        }

        var start = Math.Max(0, queryIndex - maxLines / 2);
        var count = Math.Min(lines.Length - start, maxLines);
        
        return string.Join('\n', lines.Skip(start).Take(count));
    }

    private string SanitizeName(string name)
    {
        return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray())
            .ToLowerInvariant();
    }
}
