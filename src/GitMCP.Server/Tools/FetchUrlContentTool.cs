using GitMCP.Server.Core.Interfaces;
using System.Text.Json;

namespace GitMCP.Server.Tools;

/// <summary>
/// 获取 URL 内容工具（通用）
/// </summary>
public class FetchUrlContentTool : IMcpTool
{
    private static readonly HttpClient _httpClient = new();

    public string Name => "fetch_generic_url_content";

    public string Description => "Generic tool to fetch content from any absolute URL. Use this to retrieve referenced URLs (absolute URLs) that were mentioned in previously fetched documentation.";

    public object InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "url": {
                    "type": "string",
                    "description": "The URL of the document or page to fetch"
                },
                "maxLength": {
                    "type": "integer",
                    "description": "Maximum content length to return (default: 10000 characters)",
                    "default": 10000
                }
            },
            "required": ["url"]
        }
        """);

    public async Task<object> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.TryGetValue("url", out var urlObj))
        {
            throw new ArgumentException("Missing required argument 'url'");
        }

        var url = urlObj.ToString() ?? "";
        int maxLength = 10000;

        if (args.TryGetValue("maxLength", out var maxLengthObj))
        {
            if (maxLengthObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                maxLength = jsonElement.GetInt32();
            }
            else if (int.TryParse(maxLengthObj.ToString(), out var parsed))
            {
                maxLength = parsed;
            }
        }

        try
        {
            // 验证 URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return new
                {
                    url = url,
                    error = "Invalid URL format",
                    success = false
                };
            }

            // 只允许 HTTP/HTTPS 协议
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return new
                {
                    url = url,
                    error = "Only HTTP and HTTPS protocols are supported",
                    success = false
                };
            }

            // 设置超时
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            // 发起请求
            var response = await _httpClient.GetAsync(uri, cts.Token);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";

            // 限制内容长度
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength) + "\n\n[Content truncated...]";
            }

            return new
            {
                url = url,
                success = true,
                contentType = contentType,
                size = content.Length,
                content = content,
                statusCode = (int)response.StatusCode
            };
        }
        catch (HttpRequestException ex)
        {
            return new
            {
                url = url,
                error = $"HTTP request failed: {ex.Message}",
                success = false
            };
        }
        catch (TaskCanceledException)
        {
            return new
            {
                url = url,
                error = "Request timeout (30 seconds)",
                success = false
            };
        }
        catch (Exception ex)
        {
            return new
            {
                url = url,
                error = $"Failed to fetch content: {ex.Message}",
                success = false
            };
        }
    }
}
