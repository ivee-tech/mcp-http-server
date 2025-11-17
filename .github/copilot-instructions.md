# Plan to generate a minimal viable MCP server in ASP.NET Core
- Scaffold new ASP.NET Core minimal API project (dotnet new webapi) under src/; enable nullable, implicit usings, and configure launchSettings.json for HTTPS.
- Add MCP server abstractions: define models for Tool definition/request/response, and register minimal dependency injection services for tool routing.
- Implement single "hello_world" tool class returning a static payload, expose metadata (name, description, input schema) per MCP spec, and wire it into a lightweight tool registry.
- Create HTTP endpoints under /mcp for GET /tools (enumeration) and POST /execute (dispatch by tool name); include input validation and consistent JSON responses.
- Add integration tests using WebApplicationFactory verifying tool discovery and execution, plus lint/build tasks (dotnet format, dotnet test) in a README.md quickstart.
- Document setup and usage: prerequisites (.NET SDK), build/run commands, sample curl calls, and notes on extending with additional tools. Optional next step: add OpenAPI description or logging middleware.
- Ensure the MCP Server uses HTTP Streamable responses for tool execution results to support large payloads efficiently, not SSE.