# GitMCP.Server

基于 C# 实现的 Model Context Protocol (MCP) 服务器，用于为 AI 助手提供 GitHub 仓库的文档和代码搜索功能。

## 功能特性

- ✅ **自动仓库扫描**：启动时自动扫描指定目录下的所有 Git 仓库
- ✅ **自动更新代码**：启动时自动 pull 最新代码（可配置）
- ✅ **动态工具注册**：为每个仓库自动注册文档搜索和代码搜索工具
- ✅ **MCP 协议支持**：完整支持 MCP over HTTP (SSE) 协议
- ✅ **并发更新**：支持多个仓库并发更新，提高启动速度

## 快速开始

### 1. 准备仓库目录

创建一个目录来存放你的 Git 仓库，例如：

```
D:/GitRepos/
├── react/          # React 官方仓库
├── vue/            # Vue 官方仓库
├── your-project/   # 你的项目
└── ...
```

### 2. 配置应用

编辑 `appsettings.json`，设置仓库目录：

```json
{
  "GitMCP": {
    "RepositoryBaseDirectory": "D:/GitRepos",
    "PullOnStartup": true,
    "PullTimeoutSeconds": 300,
    "MaxConcurrentPulls": 3
  }
}
```

### 3. 运行服务

```bash
cd d:\Codes\git-mcp-net\src\GitMCP.Server
dotnet run
```

服务将在 `http://localhost:3001` 启动。

### 4. 验证服务

访问以下端点：

- **首页**：`http://localhost:3001/` - 查看所有仓库和工具
- **健康检查**：`http://localhost:3001/health` - 检查服务状态
- **MCP 端点**：`http://localhost:3001/mcp/sse` - MCP 协议端点

## 配置说明

### appsettings.json

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `RepositoryBaseDirectory` | 仓库基础目录 | `D:/GitRepos` |
| `PullOnStartup` | 启动时是否更新仓库 | `true` |
| `PullTimeoutSeconds` | Pull 超时时间（秒） | `300` |
| `MaxConcurrentPulls` | 最大并发 Pull 数 | `3` |
| `ServerInfo.Name` | 服务器名称 | `GitMCP.Server` |
| `ServerInfo.Version` | 服务器版本 | `1.0.0` |

## 🛠️ Main Tools (API)

系统为每个仓库自动注册以下工具：

### 工具列表

| 工具名称 | 类型 | 描述 |
|---------|------|------|
| `fetch_{repo}_docs` | 文档获取 | Fetch entire documentation file from repository: {repo}. Useful for general questions. Always call this tool first if asked about {repo}. |
| `search_{repo}_docs` | 文档搜索 | Semantically search within the fetched documentation from repository: {repo}. Useful for specific queries. |
| `search_{repo}_code` | 代码搜索 | Search for code within the repository: "{repo}" using semantic search. Returns matching code snippets and files for you to query further if relevant. |
| `fetch_generic_url_content` | URL 获取（全局） | Generic tool to fetch content from any absolute URL. Use this to retrieve referenced URLs (absolute URLs) that were mentioned in previously fetched documentation. |

### 工具注册规则

- **仓库工具**：每个仓库注册 3 个工具（fetch_docs、search_docs、search_code）
- **全局工具**：`fetch_generic_url_content` 只注册 1 次，所有仓库共享

**示例**：如果有 2 个仓库（react、vue），将注册：
- 1 个全局工具：`fetch_generic_url_content`
- 6 个仓库工具：
  - `fetch_react_docs`、`search_react_docs`、`search_react_code`
  - `fetch_vue_docs`、`search_vue_docs`、`search_vue_code`
- **总计**：7 个工具

---

## 自动注册的工具

对于每个仓库，系统会自动注册以下工具：

### 1. 获取主文档工具

**工具名称**：`fetch_{仓库名}_docs`

**描述**：Fetch entire documentation file from repository: {仓库名}. Useful for general questions. Always call this tool first if asked about {仓库名}.

**参数**：
- `section` (string, optional): 可选的文档章节名称，用于提取特定部分

**文档优先级**：
1. `llms.txt` - AI 优化文档
2. `README.md` - 根目录自述文件
3. `docs/README.md` - 文档目录自述文件
4. `docs/index.md` - 文档索引

**示例**：
```json
{
  "name": "fetch_react_docs",
  "arguments": {
    "section": "Getting Started"
  }
}
```

### 2. 文档搜索工具

**工具名称**：`search_{仓库名}_docs`

**描述**：Semantically search within the fetched documentation from repository: {仓库名}. Useful for specific queries.

**参数**：
- `query` (string, required): The search query to find relevant documentation

**示例**：
```json
{
  "name": "search_react_docs",
  "arguments": {
    "query": "useState hook"
  }
}
```

### 3. 代码搜索工具

**工具名称**：`search_{仓库名}_code`

**描述**：Search for code within the repository: "{仓库名}" using semantic search. Returns matching code snippets and files for you to query further if relevant.

**参数**：
- `query` (string, required): The search query to find relevant code files
- `fileExtension` (string, optional): 文件扩展名过滤（如 "cs", "js", "tsx"）

**支持的文件类型**：
- **前端**：`.js`, `.ts`, `.jsx`, `.tsx`, `.vue`, `.svelte`
- **后端**：`.cs`, `.py`, `.java`, `.go`, `.rs`, `.rb`, `.php`
- **系统级**：`.cpp`, `.c`, `.h`, `.swift`, `.kt`, `.scala`

**示例**：
```json
{
  "name": "search_react_code",
  "arguments": {
    "query": "useState",
    "fileExtension": "tsx"
  }
}
```

---

### 4. 通用 URL 获取工具（全局工具）

**工具名称**：`fetch_generic_url_content`

**描述**：Generic tool to fetch content from any absolute URL. Use this to retrieve referenced URLs (absolute URLs) that were mentioned in previously fetched documentation.

**参数**：
- `url` (string, required): The URL of the document or page to fetch
- `maxLength` (integer, optional): 最大返回内容长度（默认 10000 字符）

**安全限制**：
- 仅支持 HTTP/HTTPS 协议
- 30 秒超时限制
- 内容长度限制

**示例**：
```json
{
  "name": "fetch_generic_url_content",
  "arguments": {
    "url": "https://react.dev/learn/hooks-overview",
    "maxLength": 5000
  }
}
```

**注意**：此工具为全局工具，在所有仓库间共享，只注册一次。

## 与 AI 助手集成

### Claude Desktop

在 Claude Desktop 的配置文件中添加：

```json
{
  "mcpServers": {
    "gitmcp": {
      "url": "http://localhost:3001/mcp/sse"
    }
  }
}
```

### Cursor / VS Code

在 MCP 配置中添加服务器：

```json
{
  "servers": {
    "gitmcp": {
      "url": "http://localhost:3001/mcp/sse",
      "transport": "sse"
    }
  }
}
```

## 项目结构

```
GitMCP.Server/
├── Program.cs                          # 主程序入口
├── appsettings.json                   # 配置文件
├── Core/
│   ├── Models/
│   │   ├── RepositoryInfo.cs         # 仓库信息模型
│   │   ├── ToolExecutionContext.cs   # 工具执行上下文
│   │   └── GitMcpConfiguration.cs    # 配置模型
│   └── Interfaces/
│       ├── IMcpTool.cs               # 工具接口
│       └── IToolRegistry.cs          # 工具注册器接口
├── GitHub/
│   ├── GitRepositoryScanner.cs       # 仓库扫描器
│   └── GitRepositoryUpdater.cs       # 仓库更新器
├── Tools()
│   ├── SimpleToolRegistry.cs         # 工具注册器实现
│   ├── DefaultToolProvider.cs        # 默认工具提供者
│   ├── FetchDocumentationTool.cs   # 主文档获取工具
│   ├── SearchDocumentationTool.cs    # 文档搜索工具
│   └── SearchCodeTool.cs             # 代码搜索工具
└── Services/
    └── (保留用于未来扩展)
```

## 技术栈

- **.NET 8.0**：核心框架
- **LibGit2Sharp**：Git 操作库
- **ModelContextProtocol.AspNetCore**：MCP 协议支持
- **ASP.NET Core**：Web 框架

## 开发指南

### 添加自定义工具

1. 创建实现 `IMcpTool` 接口的类：

```csharp
public class MyCustomTool : IMcpTool
{
    public string Name => "my_custom_tool";
    public string Description => "我的自定义工具";
    
    public object InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "param1": { "type": "string" }
            }
        }
        """);
    
    public async Task<object> ExecuteAsync(
        Dictionary<string, object> args, 
        CancellationToken cancellationToken = default)
    {
        // 实现工具逻辑
        return new { result = "success" };
    }
}
```

2. 在 `DefaultToolProvider` 中注册：

```csharp
public IEnumerable<IMcpTool> CreateToolsForRepository(RepositoryInfo repository)
{
    yield return new SearchDocumentationTool(repository);
    yield return new SearchCodeTool(repository);
    yield return new MyCustomTool(repository);  // 添加自定义工具
}
```

## 故障排除

### 问题 1：仓库未被扫描到

**原因**：目录下没有 `.git` 文件夹

**解决**：确保仓库是有效的 Git 仓库，包含 `.git` 目录

### 问题 2：Pull 失败

**原因**：网络问题或认证问题

**解决**：
- 检查网络连接
- 确保有访问仓库的权限
- 可以设置 `PullOnStartup: false` 跳过自动更新

### 问题 3：工具未注册

**原因**：仓库扫描失败

**解决**：查看启动日志，确认仓库是否被正确扫描

## 日志

服务启动时会输出详细日志：

```
[INFO] GitMCP Server 启动中...
[INFO] 仓库目录: D:/GitRepos
[INFO] 开始扫描仓库目录: D:/GitRepos
[INFO] 发现仓库: react (D:/GitRepos/react)
[INFO] 发现仓库: vue (D:/GitRepos/vue)
[INFO] 扫描完成，共发现 2 个仓库
[INFO] 开始更新 2 个仓库...
[INFO] 开始 pull 仓库: D:/GitRepos/react
[INFO] 仓库已是最新: D:/GitRepos/react
[INFO] 仓库更新完成: 2/2
[INFO] 开始注册工具...
[INFO] 工具注册完成，共注册 7 个工具
[INFO]   - fetch_generic_url_content: Generic tool to fetch content from any absolute URL
[INFO]   - fetch_react_docs: Fetch entire documentation file from repository: react
[INFO]   - search_react_docs: Semantically search within the fetched documentation from repository: react
[INFO]   - search_react_code: Search for code within the repository: "react" using semantic search
[INFO]   - fetch_vue_docs: Fetch entire documentation file from repository: vue
[INFO]   - search_vue_docs: Semantically search within the fetched documentation from repository: vue
[INFO]   - search_vue_code: Search for code within the repository: "vue" using semantic search
[INFO] GitMCP Server 已启动，监听端口: http://localhost:3001
```

## 性能优化

- **并发更新**：通过 `MaxConcurrentPulls` 配置并发数量
- **文件限制**：代码搜索默认限制扫描 100 个文件
- **结果限制**：默认返回最多 5-10 条结果
- **目录排除**：自动排除 `node_modules`、`bin`、`obj` 等常见目录

## 许可证

Apache License 2.0

## 贡献

欢迎提交 Issue 和 Pull Request！

## 相关链接

- [Model Context Protocol](https://modelcontextprotocol.io)
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp)
- [GitMCP 原项目](https://gitmcp.io)
