# MCP HTTP Server (ASP.NET Core)

Sample implementation of a Model Context Protocol (MCP) HTTP server built with ASP.NET Core minimal APIs. It exposes a single `hello_world` tool to demonstrate tool registration, discovery, and execution with stream-friendly JSON responses.

## Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download)

## Quickstart

```pwsh
# Restore dependencies
cd c:/s/AI/mcp-http-server
dotnet restore

# Run the server (HTTPS by default)
dotnet run --project src/McpHttpServer/McpHttpServer.csproj
```

The service listens on both HTTP and HTTPS endpoints defined in `Properties/launchSettings.json` and supports JSON-RPC requests at `/`.

## Tool discovery

```pwsh
curl http://localhost:5248/tools
```

Sample response:

```json
{
  "tools": [
    {
      "name": "hello_world",
      "description": "Returns a friendly greeting. Provide an optional 'name' field in the input.",
      "inputSchema": {
        "type": "object",
        "title": "HelloWorldInput",
        "additionalProperties": false,
        "properties": {
          "name": {
            "type": "string",
            "description": "Optional name to include in the greeting."
          }
        }
      }
    }
  ]
}
```

## Executing a tool

```pwsh
curl https://localhost:7272/mcp/execute `
  -H "Content-Type: application/json" `
  -d '{"tool":"hello_world","input":{"name":"Azure"}}' `
  -k
```

The server streams the JSON response to the caller:

```json
{"tool":"hello_world","contentType":"application/json","result":{"message":"Hello, Azure!"}}
```

## JSON-RPC usage

VS Code and other MCP-aware clients can communicate with the server via JSON-RPC 2.0 on the root endpoint:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "hello_world",
    "arguments": {
      "name": "Azure"
    }
  }
}
```

The server replies with:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tool": "hello_world",
    "content": [
      {
        "type": "json",
        "json": {
          "message": "Hello, Azure!"
        }
      }
    ]
  }
}
```

## Testing and formatting

```pwsh
# Run integration tests
dotnet test

# Format the codebase (requires dotnet-format global tool)
dotnet format
```

## Extending

- Register additional MCP tools by implementing `IMcpTool` and adding them to the DI container.
- Consider adding structured logging or OpenAPI metadata as follow-up enhancements.
