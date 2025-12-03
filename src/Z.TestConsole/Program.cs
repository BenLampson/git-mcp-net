using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;



McpServerOptions options = new()
{

};


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation { Name = "MyServer", Version = "1.0.0" };
    options.Handlers = new McpServerHandlers()
    {
        ListToolsHandler = (request, cancellationToken) =>
            ValueTask.FromResult(new ListToolsResult
            {
                Tools =
                [
                    new Tool
                    {
                        Name = "echo",
                        Description = "Echoes the input back to the client.",
                        InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                            {
                                "type": "object",
                                "properties": {
                                  "message": {
                                    "type": "string",
                                    "description": "The input to echo back"
                                  }
                                },
                                "required": ["message"]
                            }
                            """),
                    }
                ]
            }),

        CallToolHandler = (request, cancellationToken) =>
        {
            if (request.Params?.Name == "echo")
            {
                if (request.Params.Arguments?.TryGetValue("message", out var message) is not true)
                {
                    throw new McpProtocolException("Missing required argument 'message'", McpErrorCode.InvalidParams);
                }

                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = $"Echo: {message}" }]
                });
            }

            throw new McpProtocolException($"Unknown tool: '{request.Params?.Name}'", McpErrorCode.InvalidRequest);
        }
    };
})
    .WithHttpTransport();
//.WithToolsFromAssembly();
var app = builder.Build();

app.MapMcp();

app.Run("http://localhost:3001");

//[McpServerToolType]
//public static class EchoTool
//{
//    [McpServerTool, Description("Echoes the message back to the client.")]
//    public static string Echo(string message) => $"hello {message}";
//}