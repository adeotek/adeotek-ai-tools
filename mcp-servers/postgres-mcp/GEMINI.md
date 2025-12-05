# PostgreSQL MCP Server - Context for Gemini

This document provides comprehensive technical context about the PostgreSQL MCP Server for Gemini (both CLI and Web) interactions. It covers the MCP Protocol v2.0 implementation, architecture, security, and development guidelines.

## Project Overview

**Purpose**: A **read-only** Model Context Protocol (MCP) server for PostgreSQL database operations. This is a simple, lightweight MCP server implementing the MCP Protocol v2.0 specification, providing secure, read-only access to PostgreSQL databases for AI agents without requiring AI/LLM dependencies.

**Version**: 2.0.0

**MCP Protocol**: 2024-11-05 (Full JSON-RPC 2.0 support with SSE notifications, Resources, and Prompts)

**Technology Stack**: .NET 10, ASP.NET Core 10, Npgsql 8+, Serilog, Scalar, AspNetCoreRateLimit

**Location in Repository**: `/mcp-servers/postgres-mcp`

**Key Capabilities**:
- Full MCP Protocol v2.0 implementation
- Two MCP Tools: database scanning and read-only query execution
- Server-Sent Events (SSE) for real-time notifications
- Resources system for database connections and schema information
- Prompts system with 4 built-in database query templates
- JSON-RPC 2.0 compliant request/response handling
- Comprehensive read-only security with multiple validation layers
- Rate limiting, query timeouts, and row limits
- No AI/LLM dependencies required

**Key Difference from postgres-nl-mcp**: This server does NOT use AI/LLM for query generation. It provides direct SQL query execution with strict read-only validation. For AI-powered natural language queries, see `/mcp-servers/postgres-nl-mcp`.

## Architecture

### Project Structure

```
postgres-mcp/
├── src/
│   ├── PostgresMcp/              # Main application
│   │   ├── Controllers/          # MCP API endpoints
│   │   │   └── McpProtocolEndpoints.cs  # JSON-RPC 2.0 and SSE endpoints
│   │   ├── Models/               # Data models (organized by concern)
│   │   │   ├── JsonRpcModels.cs           # JSON-RPC 2.0 request/response
│   │   │   ├── McpProtocolModels.cs       # MCP v2.0 types (tools, resources, prompts)
│   │   │   ├── ServerConnectionOptions.cs # Server initialization options
│   │   │   ├── DatabaseModels.cs          # Database schema models
│   │   │   ├── PostgresOptions.cs         # PostgreSQL configuration
│   │   │   └── SecurityOptions.cs         # Security settings
│   │   ├── Services/             # Business logic (organized by concern)
│   │   │   ├── DatabaseService.cs              # Database operations
│   │   │   ├── QueryValidationService.cs      # SQL query validation
│   │   │   ├── ConnectionService.cs           # Connection management
│   │   │   ├── ISseNotificationService.cs     # SSE notification interface
│   │   │   ├── SseNotificationService.cs      # SSE implementation
│   │   │   ├── IResourceProvider.cs           # Resource provider interface
│   │   │   ├── ResourceProvider.cs            # Resource implementation
│   │   │   ├── IPromptProvider.cs             # Prompt provider interface
│   │   │   └── PromptProvider.cs              # Prompt implementation
│   │   ├── Program.cs            # Application entry point
│   │   ├── appsettings.json      # Default configuration
│   │   └── appsettings.Development.json # Development overrides
│   └── PostgresMcp.Tests/        # Unit tests
├── tests/
│   └── PostgresMcp.Tests/        # xUnit integration and unit tests
├── examples/
│   ├── sample-requests.md        # All JSON-RPC 2.0 request examples
│   └── INTEGRATION_GUIDE.md      # Integration and extension guide
├── .vscode/
│   ├── mcp-settings.json         # VS Code MCP configuration
│   ├── launch.json               # Debug configuration
│   └── tasks.json                # Build and test tasks
├── docker-init/                  # Database initialization scripts
├── Dockerfile                    # Docker build (multi-stage)
├── docker-compose.yml            # Docker orchestration
├── Makefile                      # Build automation
├── README.md                     # User-facing documentation
└── GEMINI.md                     # This file - Gemini context
```

### Design Patterns

1. **Dependency Injection**: All services registered in ASP.NET Core DI container
2. **Options Pattern**: Configuration via strongly-typed `IOptions<T>` for PostgresOptions, SecurityOptions, ServerConnectionOptions
3. **Repository Pattern**: Database access abstraction (DatabaseService)
4. **Validation Pattern**: Multi-layer query validation for security (QueryValidationService)
5. **SSE Pub/Sub Pattern**: Real-time notifications via Server-Sent Events (SseNotificationService)
6. **Provider Pattern**: Pluggable resource and prompt providers (IResourceProvider, IPromptProvider)
7. **Prompt Template Pattern**: Built-in prompts with argument substitution (PromptProvider)
8. **JSON-RPC 2.0 Pattern**: Standard JSON-RPC request/response with proper error codes

### Key Components

**Controllers** (`Controllers/McpProtocolEndpoints.cs`):
- JSON-RPC 2.0 endpoint at `POST /mcp/v1/messages`
- Server-Sent Events endpoint at `GET /mcp/v1/sse`
- MCP server discovery at `GET /.well-known/mcp.json`
- Input validation and error handling
- Rate limiting middleware integration

**Services**:
- **DatabaseService**: Database queries, schema introspection, connection pooling
- **QueryValidationService**: Multi-layer SQL validation for read-only compliance
- **ConnectionService**: Connection string parsing, connection pooling management
- **SseNotificationService**: Real-time event streaming to connected clients
- **ResourceProvider**: Database resources (connections, databases, schemas)
- **PromptProvider**: Built-in query templates with dynamic argument substitution

**Models**:
- **JsonRpcModels**: JSON-RPC 2.0 request/response structures
- **McpProtocolModels**: MCP v2.0 types (Tool, Resource, Prompt, TextContent, etc.)
- **ServerConnectionOptions**: Server initialization and connection management
- **DatabaseModels**: Schema representation (tables, columns, foreign keys, indexes)
- **PostgresOptions**: PostgreSQL connection and behavior configuration
- **SecurityOptions**: Rate limiting, query limits, timeout configuration

## MCP Protocol v2.0 Implementation

This server implements the full MCP Protocol v2.0 specification (2024-11-05) with JSON-RPC 2.0 request/response handling.

### Supported MCP Methods

#### Lifecycle Methods

**`initialize`** (Client -> Server)
Initialize the MCP connection with server capabilities and metadata.

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "clientInfo": {
      "name": "my-client",
      "version": "1.0.0"
    }
  }
}
```

Response includes server metadata, capabilities, and available tools/resources/prompts.

**`initialized`** (Client -> Server)
Confirmation that client has finished initialization.

**`ping`** (Client <-> Server, bidirectional)
Keep-alive heartbeat message. Server responds with pong after 30-second intervals.

#### Tool Methods

**`tools/list`** (Client -> Server)
Returns list of all available MCP tools with schemas.

**`tools/call`** (Client -> Server)
Executes a specific tool with provided arguments.

#### Resource Methods

**`resources/list`** (Client -> Server)
Lists all available database resources (connections, databases, schemas).

**`resources/read`** (Client -> Server)
Reads the content of a specific resource (e.g., schema information).

**`resources/subscribe`** (Client -> Server)
Subscribe to real-time updates for a resource via SSE.

**`resources/unsubscribe`** (Client -> Server)
Unsubscribe from resource updates.

#### Prompt Methods

**`prompts/list`** (Client -> Server)
Lists all available prompt templates.

**`prompts/get`** (Client -> Server)
Get a specific prompt with argument placeholders replaced.

### JSON-RPC 2.0 Error Codes

The server implements standard JSON-RPC 2.0 error codes:

- **-32700**: Parse error - JSON parsing error
- **-32600**: Invalid request - Malformed JSON-RPC request
- **-32601**: Method not found - Unknown method
- **-32602**: Invalid params - Invalid method parameters
- **-32603**: Internal error - Server internal error
- **-32000 to -32099**: Server errors (custom error range)

Example error response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": "Query validation failed: blocked keyword detected"
  }
}
```

## MCP Tools

This server provides **two** MCP tools (compared to three in postgres-nl-mcp):

### Tool 1: scan_database_structure

Scan and analyze PostgreSQL database structure in detail.

**Capabilities**:
- List all tables with their schemas
- Display columns with data types, constraints, defaults
- Show primary keys and foreign keys
- List indexes (unique, non-unique)
- Identify table relationships and cardinality
- Provide row count estimates
- No AI/LLM required - direct schema introspection

**Input Schema**:
```json
{
  "connectionString": "Host=...;Database=...;Username=...;Password=...",
  "schemaFilter": "public"  // optional, defaults to "public"
}
```

**Example Use Cases**:
- "What tables exist in the database?"
- "Show me the structure of the customers table"
- "What foreign keys reference the orders table?"
- "List all indexes on the products table"

### Tool 2: query_database

Execute read-only SELECT queries against the database.

**Capabilities**:
- Execute SELECT queries only (strict validation)
- Automatic safety validation (multiple layers)
- Row limit enforcement (configurable, default 10,000)
- Query timeout protection (configurable, default 30 seconds)
- Execution time tracking
- Result truncation for large datasets
- No AI/LLM required - direct SQL execution

**Input Schema**:
```json
{
  "connectionString": "Host=...;Database=...;Username=...;Password=...",
  "query": "SELECT * FROM customers WHERE created_at > '2024-01-01' LIMIT 100"
}
```

**Example Use Cases**:
- "SELECT * FROM customers WHERE created_at > '2024-01-01'"
- "SELECT product_name, price FROM products WHERE category = 'Electronics' ORDER BY price DESC LIMIT 10"
- "SELECT c.name, COUNT(o.id) as order_count FROM customers c LEFT JOIN orders o ON c.id = o.customer_id GROUP BY c.id, c.name"

**What This Tool Does NOT Do** (compared to postgres-nl-mcp):
- ❌ No natural language to SQL conversion (requires manual SQL)
- ❌ No automatic relationship detection (must write JOINs manually)
- ❌ No AI-powered query generation
- ❌ No query optimization suggestions

## Server-Sent Events (SSE)

This server supports real-time notifications via Server-Sent Events on the `/mcp/v1/sse` endpoint.

### How SSE Works

The server maintains persistent HTTP connections with clients using Server-Sent Events. Events are formatted as:
```
event: resource_updated
data: {"type": "resource", "uri": "postgres://localhost:5432/databases"}

event: connection_status
data: {"status": "connected", "timestamp": "2024-12-03T10:30:00Z"}
```

### Notification Types

**Resource Updated** (`resource_updated`):
Sent when a resource's state changes (e.g., new table added, schema modified).

**Query Executed** (`query_executed`):
Notification about query execution (in audit mode).

**Connection Status** (`connection_status`):
Server connection status changes.

**Server Heartbeat** (`heartbeat`):
Keep-alive message sent every 30 seconds to maintain connection.

### SSE Implementation Details

```csharp
// Subscribe to SSE notifications
GET /mcp/v1/sse

// Response stream headers
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive

// Server sends events continuously
event: heartbeat
data: {}
retry: 10000

event: resource_updated
data: {"uri": "postgres://localhost:5432/tables/customers"}
```

### Sending SSE Notifications in Code

```csharp
// In SseNotificationService
public async Task SendNotificationAsync(string eventType, object data, CancellationToken cancellationToken = default)
{
    var notification = new SseEvent
    {
        Type = eventType,
        Data = JsonSerializer.Serialize(data),
        Timestamp = DateTime.UtcNow
    };

    // Queue for all connected SSE clients
    await _eventQueue.WriteAsync(notification, cancellationToken);
}

// Usage
await _sseService.SendNotificationAsync("resource_updated",
    new { uri = "postgres://host:5432/schema" });
```

## Resources System

Resources in MCP v2.0 represent queryable data endpoints that clients can subscribe to for real-time updates.

### Available Resource Types

**Connection Resources** (`postgres://host:port/connection`):
```
postgres://localhost:5432/connection
```
Represents a database connection configuration.

**Database Resources** (`postgres://host:port/databases`):
```
postgres://localhost:5432/databases
postgres://localhost:5432/databases/mydb
```
Lists databases or details about a specific database.

**Schema Resources** (`postgres://host:port/schema`):
```
postgres://localhost:5432/schema/public
postgres://localhost:5432/schema/public/tables
postgres://localhost:5432/schema/public/tables/customers
```
Provides schema information for tables, columns, indexes, constraints.

### Resource URI Format

```
postgres://[host]:[port]/[resource-type]/[resource-name]
```

Example:
```
postgres://localhost:5432/databases/mydb/tables/users/columns
```

### Adding New Resources

To add a new resource type, implement `IResourceProvider`:

```csharp
public interface IResourceProvider
{
    Task<IEnumerable<Resource>> ListResourcesAsync(CancellationToken cancellationToken);
    Task<string> ReadResourceAsync(string uri, CancellationToken cancellationToken);
    Task SubscribeAsync(string uri, Func<string, Task> onUpdate, CancellationToken cancellationToken);
}
```

Then register in `Program.cs`:
```csharp
services.AddScoped<IResourceProvider, CustomResourceProvider>();
```

## Prompts System

Prompts are built-in query templates with dynamic argument substitution, helping clients generate consistent database queries.

### Built-in Prompts

**1. analyze_table**
Analyzes a specific table structure and suggests useful queries.

```json
{
  "name": "analyze_table",
  "description": "Analyze table structure and suggest queries",
  "arguments": [
    {
      "name": "table_name",
      "description": "Name of the table to analyze",
      "required": true
    },
    {
      "name": "schema",
      "description": "Schema name (default: public)",
      "required": false
    }
  ]
}
```

**2. find_relationships**
Discovers foreign key relationships and suggests JOINs.

```json
{
  "name": "find_relationships",
  "description": "Find table relationships via foreign keys",
  "arguments": [
    {
      "name": "table_name",
      "description": "Table to find relationships for",
      "required": true
    }
  ]
}
```

**3. recent_data**
Gets recent data with timestamps from a table.

```json
{
  "name": "recent_data",
  "description": "Query recent data with timestamps",
  "arguments": [
    {
      "name": "table_name",
      "description": "Table to query",
      "required": true
    },
    {
      "name": "limit",
      "description": "Number of records (default: 100)",
      "required": false
    }
  ]
}
```

**4. search_columns**
Searches for columns by name pattern across all tables.

```json
{
  "name": "search_columns",
  "description": "Find columns matching a pattern",
  "arguments": [
    {
      "name": "pattern",
      "description": "Column name pattern (e.g., 'email', 'date')",
      "required": true
    }
  ]
}
```

### How Prompts Work

Prompts work with the `prompts/get` MCP method:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "prompts/get",
  "params": {
    "name": "analyze_table",
    "arguments": {
      "table_name": "customers"
    }
  }
}
```

Response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "description": "Analyze the customers table structure",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Analyze the 'customers' table structure in the 'public' schema. Show columns, data types, constraints, and suggest useful queries."
        }
      }
    ]
  }
}
```

### Adding New Prompts

Add custom prompts in `PromptProvider`:

```csharp
public class PromptProvider : IPromptProvider
{
    public async Task<Prompt> GetPromptAsync(string name, Dictionary<string, string> arguments)
    {
        return name switch
        {
            "my_custom_prompt" => new Prompt
            {
                Name = "my_custom_prompt",
                Description = "My custom prompt",
                Arguments = new[] { /* argument definitions */ },
                Messages = new[]
                {
                    new PromptMessage
                    {
                        Role = "user",
                        Content = new TextContent
                        {
                            Text = $