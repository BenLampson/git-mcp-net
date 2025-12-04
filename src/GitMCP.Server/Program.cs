using GitMCP.Server.Core.Interfaces;
using GitMCP.Server.Core.Models;
using GitMCP.Server.GitHub;
using GitMCP.Server.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 加载配置
var config = builder.Configuration.GetSection("GitMCP").Get<GitMcpConfiguration>() ?? new GitMcpConfiguration();

// 注册服务
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<GitRepositoryScanner>();
builder.Services.AddSingleton<GitRepositoryUpdater>();
builder.Services.AddSingleton<IToolRegistry, SimpleToolRegistry>();
builder.Services.AddSingleton<DefaultToolProvider>();

var app = builder.Build();

// 启动时扫描和更新仓库
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var scanner = app.Services.GetRequiredService<GitRepositoryScanner>();
var updater = app.Services.GetRequiredService<GitRepositoryUpdater>();
var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
var toolProvider = app.Services.GetRequiredService<DefaultToolProvider>();

logger.LogInformation("GitMCP Server 启动中...");
logger.LogInformation("仓库目录: {Directory}", config.RepositoryBaseDirectory);

// 扫描本地仓库
var repositories = scanner.ScanLocalRepositories(config.RepositoryBaseDirectory);

if (repositories.Count == 0)
{
    logger.LogWarning("未发现任何仓库。请确保 {Directory} 目录下有 Git 仓库。", config.RepositoryBaseDirectory);
}
else
{
    // 更新仓库
    if (config.PullOnStartup)
    {
        logger.LogInformation("开始更新 {Count} 个仓库...", repositories.Count);
        var successCount = await updater.PullMultipleAsync(repositories, config.MaxConcurrentPulls);
        logger.LogInformation("仓库更新完成: {Success}/{Total}", successCount, repositories.Count);
    }

    // 注册工具
    logger.LogInformation("开始注册工具...");
    
    // 注册全局工具
    var globalTools = toolProvider.CreateGlobalTools();
    foreach (var tool in globalTools)
    {
        toolRegistry.RegisterTool(tool);
    }

    // 注册仓库特定工具
    foreach (var repo in repositories)
    {
        var tools = toolProvider.CreateToolsForRepository(repo);
        foreach (var tool in tools)
        {
            toolRegistry.RegisterTool(tool);
        }
    }

    var allTools = toolRegistry.GetAllTools().ToList();
    logger.LogInformation("工具注册完成，共注册 {Count} 个工具", allTools.Count);
    foreach (var tool in allTools)
    {
        logger.LogInformation("  - {ToolName}: {Description}", tool.Name, tool.Description);
    }
}

// 添加 MCP 服务器（在扫描仓库之后，这样可以访问 toolRegistry）
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation 
    { 
        Name = config.ServerInfo.Name, 
        Version = config.ServerInfo.Version 
    };
    
    options.Handlers = new McpServerHandlers
    {
        ListToolsHandler = (request, cancellationToken) =>
        {
            var tools = toolRegistry.GetAllTools().Select(t => new Tool
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema as JsonElement? ?? JsonSerializer.SerializeToElement(t.InputSchema)
            }).ToList();

            return ValueTask.FromResult(new ListToolsResult { Tools = tools });
        },        CallToolHandler = async (request, cancellationToken) =>
        {
            var toolName = request.Params?.Name;
            if (string.IsNullOrEmpty(toolName))
            {
                throw new McpProtocolException("工具名称不能为空", McpErrorCode.InvalidParams);
            }

            var tool = toolRegistry.GetTool(toolName);
            if (tool == null)
            {
                throw new McpProtocolException($"未找到工具: {toolName}", McpErrorCode.InvalidRequest);
            }

            // 转换参数类型
            var args = new Dictionary<string, object>();
            if (request.Params?.Arguments != null)
            {
                foreach (var kvp in request.Params.Arguments)
                {
                    args[kvp.Key] = kvp.Value;
                }
            }
            
            try
            {
                var result = await tool.ExecuteAsync(args, cancellationToken);
                var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = resultJson }]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "工具执行失败: {ToolName}", toolName);
                throw new McpProtocolException($"工具执行失败: {ex.Message}", McpErrorCode.InternalError);
            }
        }
    };
})
.WithHttpTransport();

// 配置 MCP 端点
app.MapMcp();

// 添加健康检查端点
app.MapGet("/health", () => new
{
    status = "healthy",
    repositories = repositories.Count,
    tools = toolRegistry.GetAllTools().Count(),
    timestamp = DateTime.UtcNow
});

app.MapGet("/", () => new
{
    name = config.ServerInfo.Name,
    version = config.ServerInfo.Version,
    repositories = repositories.Select(r => new { r.Name, r.RemoteUrl, r.CurrentBranch }),
    tools = toolRegistry.GetAllTools().Select(t => new { t.Name, t.Description })
});

logger.LogInformation("GitMCP Server 已启动，监听端口: http://localhost:3001");
logger.LogInformation("健康检查: http://localhost:3001/health");
logger.LogInformation("MCP 端点: http://localhost:3001/mcp/sse");

app.Run("http://localhost:3001");

