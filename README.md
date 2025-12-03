# GitMCP C# Replica

This project aims to replicate the core features of [GitMCP](https://gitmcp.io) in C#, enabling AI assistants to seamlessly access documentation and code from any GitHub project, eliminating code hallucinations and improving development efficiency.

---

## 📦 Feature List (Aligned with the Original)

- **Latest Documentation Access**: AI assistants can fetch up-to-date documentation and code from any GitHub project.
- **Intelligent Search**: Supports smart retrieval of both documentation and code, precisely locating required content.
- **Zero-Config Cloud Service**: No local installation needed, remote invocation via the MCP protocol.
- **Embedded Chat**: Interact with project documentation via web or IDE-embedded chat windows.
- **Open Source & Free**: Fully open source, self-hosting supported, privacy protected.
- **Multiple Integration Methods**: Compatible with major AI assistants such as Cursor, VSCode, Claude, Windsurf, Highlight AI, Augment Code, Msty AI, etc.
- **Multiple Documentation Formats**: Prioritizes `llms.txt`, then optimized project docs, then `README.md`.
- **Badge & Statistics**: Add a badge to your project to show documentation access statistics.
- **Privacy Protection**: No collection or storage of any personal information or queries.
- **robots.txt Compliance**: Automatically checks and respects robots.txt rules before accessing GitHub Pages.

---

## 🚀 Getting Started

1. Select your target GitHub repository and configure the MCP server address.
2. Add the MCP server to your AI assistant, choosing dedicated or generic mode as needed.
3. Ask questions via your AI assistant to automatically retrieve the latest documentation and code snippets.

---

## 🛠️ Main Tools (API)

- `fetch_<repo-name>_documentation`: Fetch the main documentation of the specified repository.
- `search_<repo-name>_documentation`: Smart search within repository documentation.
- `fetch_url_content`: Retrieve content from external links referenced in documentation.
- `search_<repo-name>_code`: Search for code implementations and examples in the repository.

---

## 📝 Replica Notes

This project will gradually implement all the above features to ensure a consistent experience with the original GitMCP. Contributions and suggestions are welcome!

---

## 📄 License

This project is licensed under the Apache License 2.0.
