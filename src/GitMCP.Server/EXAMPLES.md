# GitMCP.Server 使用示例

本文档提供了 GitMCP.Server 的实际使用示例。

## 准备工作

### 1. 创建仓库目录并克隆示例仓库

```powershell
# 创建仓库目录
mkdir D:\GitRepos

# 克隆一些常用的开源项目
cd D:\GitRepos

# React
git clone https://github.com/facebook/react.git

# Vue
git clone https://github.com/vuejs/core.git vue

# TypeScript
git clone https://github.com/microsoft/TypeScript.git

# 你自己的项目
git clone https://github.com/your-username/your-project.git
```

### 2. 启动服务

```powershell
cd d:\Codes\git-mcp-net\src\GitMCP.Server
dotnet run
```

或使用快速启动脚本：

```powershell
.\start.ps1
```

### 3. 验证服务

在浏览器中访问 `http://localhost:3001`，你应该看到类似这样的输出：

```json
{
  "name": "GitMCP.Server",
  "version": "1.0.0",
  "repositories": [
    {
      "name": "react",
      "remoteUrl": "https://github.com/facebook/react.git",
      "currentBranch": "main"
    },
    {
      "name": "vue",
      "remoteUrl": "https://github.com/vuejs/core.git",
      "currentBranch": "main"
    }
  ],
  "tools": [
    {
      "name": "search_react_documentation",
      "description": "Search documentation in the react repository..."
    },
    {
      "name": "search_react_code",
      "description": "Search code in the react repository..."
    },
    {
      "name": "search_vue_documentation",
      "description": "Search documentation in the vue repository..."
    },
    {
      "name": "search_vue_code",
      "description": "Search code in the vue repository..."
    }
  ]
}
```

## 使用 MCP Inspector 测试

### 1. 安装 MCP Inspector

```bash
npm install -g @modelcontextprotocol/inspector
```

### 2. 连接到服务

```bash
mcp-inspector http://localhost:3001/mcp/sse
```

### 3. 测试工具调用

在 Inspector 中，选择工具并测试：

#### 示例 1: 搜索 React 文档中的 useState

```json
{
  "name": "search_react_documentation",
  "arguments": {
    "query": "useState"
  }
}
```

**预期结果**：
```json
{
  "query": "useState",
  "repository": "react",
  "results": [
    {
      "file": "docs/hooks-state.md",
      "snippet": "...useState is a Hook that lets you add state to function components...",
      "repository": "react"
    }
  ],
  "total": 1
}
```

#### 示例 2: 搜索 React 代码中的 useState 实现

```json
{
  "name": "search_react_code",
  "arguments": {
    "query": "function useState",
    "fileExtension": "js"
  }
}
```

## 与 AI 助手集成

### Claude Desktop

1. 找到 Claude Desktop 配置文件：
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. 添加 MCP 服务器配置：

```json
{
  "mcpServers": {
    "gitmcp": {
      "command": "node",
      "args": ["-e", "require('http').get('http://localhost:3001/mcp/sse')"],
      "env": {}
    }
  }
}
```

或使用 SSE URL 直接连接（如果支持）：

```json
{
  "mcpServers": {
    "gitmcp": {
      "url": "http://localhost:3001/mcp/sse",
      "transport": "sse"
    }
  }
}
```

3. 重启 Claude Desktop

4. 在对话中使用：

```
问：React 中的 useState hook 是如何工作的？

Claude 会自动调用 search_react_documentation 工具来查找相关文档。
```

### Cursor / VS Code

1. 安装 MCP 扩展（如果有）

2. 在设置中添加 MCP 服务器：

```json
{
  "mcp.servers": {
    "gitmcp": {
      "url": "http://localhost:3001/mcp/sse"
    }
  }
}
```

3. 在 Cursor/VS Code 中使用 AI 助手，它会自动访问这些工具

## 实际应用场景

### 场景 1: 学习新框架

你想学习 React Hooks：

```
你: React Hooks 的基本用法是什么？

AI: [使用 search_react_documentation 工具搜索 "hooks"]
根据 React 官方文档，Hooks 是让你在函数组件中使用 state 和其他 React 特性的函数...
```

### 场景 2: 查找代码示例

你想看看 Vue 3 的组合式 API 示例：

```
你: Vue 3 组合式 API 的实际代码示例

AI: [使用 search_vue_code 工具搜索 "setup composition"]
这里是一些 Vue 3 组合式 API 的示例代码...
```

### 场景 3: 调试问题

你在使用某个库时遇到问题：

```
你: TypeScript 中的泛型约束怎么写？

AI: [使用 search_typescript_documentation 和 search_typescript_code]
TypeScript 的泛型约束可以这样写...
```

## 高级用法

### 配置多个仓库目录

如果你的仓库分散在多个目录，可以修改代码来支持多个目录扫描：

```csharp
// 在 Program.cs 中修改
var repoDirs = new[] { "D:/GitRepos", "D:/Work/Projects", "C:/OpenSource" };
var allRepos = new List<RepositoryInfo>();

foreach (var dir in repoDirs)
{
    allRepos.AddRange(scanner.ScanLocalRepositories(dir));
}
```

### 自定义工具

创建专门的工具来处理特定类型的查询：

```csharp
public class FindApiTool : IMcpTool
{
    public string Name => $"find_{_repository.Name}_api";
    public string Description => "Find API definitions and usage";
    
    public async Task<object> ExecuteAsync(
        Dictionary<string, object> args, 
        CancellationToken cancellationToken)
    {
        // 专门搜索 API 定义
        // 例如搜索函数签名、类定义等
    }
}
```

## 性能调优

### 加快启动速度

如果你有很多仓库，可以禁用启动时的 pull：

```json
{
  "GitMCP": {
    "PullOnStartup": false
  }
}
```

然后使用定时任务或手动更新仓库。

### 限制搜索范围

修改 `SearchCodeTool` 和 `SearchDocumentationTool` 来限制搜索的文件数量和大小。

## 故障排除

### 问题：工具调用返回空结果

**可能原因**：
1. 搜索关键词不在仓库中
2. 文件类型被过滤掉了
3. 目录被排除了

**解决方案**：
- 检查搜索关键词
- 调整 `SearchCodeTool` 中的文件扩展名列表
- 检查 `excludeDirs` 配置

### 问题：启动很慢

**可能原因**：仓库太多或网络慢

**解决方案**：
- 减少 `MaxConcurrentPulls`
- 设置 `PullOnStartup: false`
- 删除不需要的仓库

## 扩展阅读

- [Model Context Protocol 规范](https://spec.modelcontextprotocol.io)
- [LibGit2Sharp 文档](https://github.com/libgit2/libgit2sharp/wiki)
- [ASP.NET Core 文档](https://docs.microsoft.com/aspnet/core)

## 贡献示例代码

如果你创建了有用的自定义工具，欢迎分享！
