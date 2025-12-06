# PostgreSQL MCP Server - Context for Claude

This document provides comprehensive technical context about the PostgreSQL MCP Server for Claude (both CLI and Web) interactions. It covers the MCP Protocol v2.0 implementation, architecture, security, and development guidelines.

## Project Overview

**Purpose**: A **read-only** Model Context Protocol (MCP) server for PostgreSQL database operations. This is a simple, lightweight MCP server implementing the MCP Protocol v2.0 specification, providing secure, read-only access to PostgreSQL databases for AI agents without requiring AI/LLM dependencies.

**Version**: 2.0.0

**MCP Protocol**: 2025-11-25 (Full JSON-RPC 2.0 support with SSE notifications, Resources, and Prompts)

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
└── CLAUDE.md                     # This file - Claude context
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

This server implements the full MCP Protocol v2.0 specification (2025-11-25) with JSON-RPC 2.0 request/response handling.

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
    "protocolVersion": "2025-11-25",
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
                            Text = $"Use {arguments["template_param"]}"
                        }
                    }
                }
            },
            _ => throw new ArgumentException($"Unknown prompt: {name}")
        };
    }
}
```

## Read-Only Security

This server implements **comprehensive read-only validation** with multiple layers of protection:

### Blocked Operations

**Data Modifications**:
- `INSERT` - Adding new data
- `UPDATE` - Modifying existing data
- `DELETE` - Removing data
- `TRUNCATE` - Clearing tables
- `MERGE` / `UPSERT` - Insert or update operations
- `COPY` - Bulk data operations

**Schema Modifications**:
- `CREATE` - Creating tables, indexes, etc.
- `ALTER` - Modifying table structure
- `DROP` - Deleting database objects
- `RENAME` - Renaming database objects
- `GRANT` / `REVOKE` - Permission changes
- `COMMENT ON` - Metadata changes

**System Operations**:
- Transaction control: `BEGIN`, `COMMIT`, `ROLLBACK`, `SAVEPOINT`
- Lock statements: `LOCK TABLE`, `ADVISORY LOCK`
- Maintenance commands: `VACUUM`, `ANALYZE`, `REINDEX`, `CLUSTER`
- Configuration changes: `SET`, `RESET`
- Messaging: `LISTEN`, `NOTIFY`, `UNLISTEN`
- Dangerous functions: `pg_read_file`, `pg_execute`, `pg_terminate_backend`
- Procedural code: `DO`, `$$`, `DECLARE`, `CREATE FUNCTION`
- Foreign data wrappers: `CREATE SERVER`, `CREATE FOREIGN TABLE`

### Validation Layers

1. **Regex-based SQL parsing**: Checks for blocked keywords and patterns
2. **Schema filtering**: Blocks access to system schemas (pg_catalog, information_schema)
3. **Row limits**: Automatic LIMIT clause enforcement
4. **Query timeouts**: Prevents long-running queries
5. **Rate limiting**: Protects against abuse (configurable per IP)

## MCP Specification 2025-11-25 Compliance

### Compliance Status

This PostgreSQL MCP Server implements **MCP Protocol 2025-11-25** with the following compliance level:

**Core Features** (✅ Fully Implemented):
- JSON-RPC 2.0 base protocol with proper message format
- Lifecycle management (initialize, initialized, ping)
- Tools support (tools/list, tools/call)
- Resources support (resources/list, resources/read, resources/subscribe, resources/unsubscribe)
- Prompts support (prompts/list, prompts/get)
- Server-Sent Events (SSE) for real-time notifications
- Server discovery via `.well-known/mcp.json`
- Batch request support
- Proper error handling with JSON-RPC error codes

**New 2025-11-25 Features**:
- **Async Tasks** (⚠️ Declared but not yet implemented): Tasks capability is advertised in server capabilities as `supported: false`. This indicates the server is aware of the tasks feature but has not yet implemented the full task management API (tasks/create, tasks/get, tasks/list, tasks/cancel). Implementation is planned for a future release.
- **CIMD/OAuth** (❌ Not applicable): This server uses runtime initialization for connection configuration instead of OAuth-based authentication.
- **Official Extensions** (❌ Not yet declared): No custom protocol extensions are currently used.

### Protocol Version Negotiation

The server declares support for MCP Protocol version **2025-11-25** during the initialize handshake. Clients using older protocol versions (e.g., 2024-11-05) will continue to work as the core protocol features remain backward compatible.

### Future Roadmap

**Planned for Future Releases**:
1. **Full Async Tasks Implementation**: Complete support for long-running database operations with progress tracking, cancellation, and task state management
2. **Enhanced Server Discovery**: Additional metadata in `.well-known/mcp.json` for better registry integration
3. **Official Extensions**: Declare and document any custom protocol extensions

### Compliance Documentation

For detailed compliance analysis, see `MCP_COMPLIANCE_ANALYSIS.md` in the project root. This document includes:
- Feature-by-feature compliance status
- Gaps and missing features
- Implementation priorities
- References to MCP specification

## Configuration

### Configuration Methods

The application supports multiple configuration methods (in priority order):
1. **Runtime initialization** (via MCP `initialize` method, highest priority) - allows dynamic connection configuration
2. **Connection string in configuration** (appsettings.json or environment variables) - allows pre-configured server connection
3. **Environment variables** (other settings)
4. **User secrets** (development only)
5. **appsettings.{Environment}.json**
6. **appsettings.json** (default values, lowest priority)

**Connection String Configuration**: You can optionally configure a PostgreSQL connection string in `appsettings.json` or via environment variables. This allows the MCP server to connect to a PostgreSQL instance without runtime initialization. The connection string should specify the server instance but can omit the database parameter, allowing tools to connect to any database on that server.

**Example**:
```bash
# In .env or environment variables
Postgres__ConnectionString=Host=localhost;Port=5432;Username=postgres;Password=yourpass

# In appsettings.json
{
  "Postgres": {
    "ConnectionString": "Host=localhost;Port=5432;Username=postgres;Password=yourpass"
  }
}
```

### Environment Variables

**Server Configuration**:
```bash
ASPNETCORE_ENVIRONMENT=Development               # Development, Staging, Production
ASPNETCORE_URLS=http://+:5000;https://+:5001    # Listening URLs (.NET 10)
DOTNET_ENVIRONMENT=Development                  # Alternative to ASPNETCORE_ENVIRONMENT
```

**PostgreSQL Options**:
```bash
Postgres__ConnectionString=Host=...;Port=...;Username=...;Password=...  # Base connection string (optional)
Postgres__MaxRetries=3                           # Connection retry attempts
Postgres__ConnectionTimeoutSeconds=30            # Connection timeout
Postgres__CommandTimeoutSeconds=60               # Query execution timeout
Postgres__MaxPoolSize=100                        # Maximum connection pool size
Postgres__MinPoolSize=0                          # Minimum connection pool size
Postgres__UseSsl=true                            # Require SSL for connections
```

**Security Settings**:
```bash
Security__EnableRateLimiting=true                # Enable rate limiting
Security__RequestsPerMinute=60                   # Max requests per minute per IP
Security__MaxRowsPerQuery=10000                  # Max rows returned per query
Security__MaxQueryExecutionSeconds=30            # Query timeout
Security__AllowedSchemas=public,analytics       # Comma-separated schema whitelist
```

**Logging Configuration**:
```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__PostgresMcp=Debug
Logging__Console__IncludeScopes=false
Logging__LogQueries=false                        # Log executed SQL queries (sensitive)
Logging__LogResults=false                        # Log query results (sensitive data)
```

**MCP Server Options**:
```bash
MCP__ServerName=postgres-mcp                     # Server name in MCP metadata
MCP__ServerVersion=2.0.0                         # Version number
MCP__EnableSseNotifications=true                 # Enable real-time SSE
MCP__EnableResources=true                        # Enable Resources system
MCP__EnablePrompts=true                          # Enable Prompts system
```

### User Secrets (Development)

For local development, use .NET user secrets (never committed to version control):

```bash
cd src/PostgresMcp
dotnet user-secrets init

# Set PostgreSQL connection string for testing
dotnet user-secrets set "Postgres:ConnectionString" "Host=localhost;Port=5432;Username=postgres;Password=yourpass"
```

## Development Workflow

### Local Development Setup

1. **Install prerequisites**:
   ```bash
   # Install .NET 10 SDK (latest)
   dotnet --version  # Should show 10.x.x

   # Install PostgreSQL 16+ (or use Docker Compose)
   ```

2. **Clone and navigate**:
   ```bash
   cd mcp-servers/postgres-mcp
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run the application**:
   ```bash
   cd src/PostgresMcp
   dotnet run

   # Or for hot reload (recommended for development):
   dotnet watch run

   # Or specify environment:
   ASPNETCORE_ENVIRONMENT=Development dotnet run
   ```

5. **Access the application**:
   - **MCP Server (JSON-RPC)**: http://localhost:5000/mcp/v1/messages
   - **SSE Notifications**: http://localhost:5000/mcp/v1/sse
   - **API Documentation**: http://localhost:5000/scalar/v1
   - **Health Check**: http://localhost:5000/health
   - **Server Discovery**: http://localhost:5000/.well-known/mcp.json

### Running Tests

```bash
# Run all tests
cd mcp-servers/postgres-mcp
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~McpControllerTests"

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Run in watch mode (auto-run on file changes)
dotnet watch test
```

### Code Quality

```bash
# Format code
dotnet format

# Analyze code
dotnet build /warnaserror

# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Update NuGet packages
dotnet list package --outdated
dotnet add package <PackageName> --version <Version>
```

### Using Makefile

The project includes a Makefile for common operations:

```bash
# Build the project
make build

# Run tests
make test

# Run the application
make run

# Build Docker image
make docker-build

# Run with Docker Compose
make docker-up

# Stop Docker Compose
make docker-down

# Clean build artifacts
make clean
```

## Docker Deployment

### Using Docker Compose (Recommended)

The docker-compose setup includes:
- PostgreSQL MCP Server (port 5000)
- PostgreSQL database with sample data (port 5432)
- pgAdmin for database management (port 8080)

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f postgres-mcp

# View all logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (resets database)
docker-compose down -v

# Rebuild after code changes
docker-compose build --no-cache postgres-mcp
docker-compose up -d
```

**Access**:
- MCP Server: http://localhost:5000
- API Documentation: http://localhost:5000/scalar/v1
- PostgreSQL: localhost:5432 (postgres/password)
- pgAdmin: http://localhost:8080 (admin@admin.com/admin)

### Using Docker Directly

```bash
# Build the image
docker build -t postgres-mcp:latest .

# Run the container
docker run -d \
  -p 5000:5000 \
  -e Postgres__DefaultConnectionString="Host=host.docker.internal;Database=mydb;Username=postgres;Password=pass" \
  --name postgres-mcp \
  postgres-mcp:latest

# View logs
docker logs -f postgres-mcp

# Stop and remove
docker stop postgres-mcp
docker rm postgres-mcp
```

## API Endpoints

### Primary Endpoints (MCP Protocol v2.0)

#### `POST /mcp/v1/messages`

**Primary JSON-RPC 2.0 endpoint** for all MCP client communication. Supports all MCP methods: initialize, tools/list, tools/call, resources/list, resources/read, prompts/list, prompts/get, etc.

**Example - Initialize**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-11-25",
    "clientInfo": {
      "name": "my-client",
      "version": "1.0.0"
    }
  }
}
```

**Example - Call Tool**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "query_database",
    "arguments": {
      "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass",
      "query": "SELECT * FROM customers LIMIT 10"
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "success": true,
    "data": {
      "rows": [...],
      "rowCount": 10,
      "executionTimeMs": 45
    }
  }
}
```

#### `GET /mcp/v1/sse`

**Server-Sent Events endpoint** for real-time notifications. Maintains persistent HTTP connection for streaming events.

**Usage**:
```bash
curl -N "http://localhost:5000/mcp/v1/sse"
```

**Events Streamed**:
- `heartbeat` - Keep-alive (every 30 seconds)
- `resource_updated` - Database resource changes
- `connection_status` - Connection state changes
- `query_executed` - Query execution notifications (in audit mode)

#### `GET /.well-known/mcp.json`

**Server discovery endpoint** for MCP client autodiscovery. Returns server metadata and capabilities.

**Response**:
```json
{
  "serverName": "postgres-mcp",
  "serverVersion": "2.0.0",
  "protocolVersion": "2025-11-25",
  "capabilities": {
    "tools": true,
    "resources": true,
    "prompts": true,
    "sseNotifications": true
  },
  "endpoint": "http://localhost:5000/mcp/v1/messages"
}
```

### Legacy Endpoints (Deprecated but Supported)

#### `GET /mcp/tools`

Lists all available MCP tools (RESTful style, not JSON-RPC 2.0).

#### `POST /mcp/tools/call`

Executes an MCP tool via REST (not JSON-RPC 2.0).

#### `POST /mcp/jsonrpc` (deprecated)

Legacy JSON-RPC endpoint. Use `/mcp/v1/messages` instead.

### Utility Endpoints

#### `GET /health`

Health check endpoint.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-12-03T10:30:00Z",
  "version": "2.0.0",
  "protocolVersion": "2025-11-25"
}
```

#### `GET /scalar/v1`

Beautiful interactive API documentation powered by Scalar.

## Comparison with postgres-nl-mcp

It's important to understand the differences between the two PostgreSQL MCP servers:

| Feature | postgres-mcp (this project) | postgres-nl-mcp |
|---------|----------------------------|-----------------|
| **Purpose** | Direct read-only SQL access | AI-powered natural language queries |
| **AI/LLM Required** | ❌ No | ✅ Yes (OpenAI, Anthropic, Gemini, Ollama, LM Studio) |
| **Query Input** | Manual SQL only | Natural language OR SQL |
| **Query Generation** | ❌ None | ✅ AI-powered natural language to SQL |
| **Query Optimization** | ❌ None | ✅ AI-powered optimization suggestions |
| **Relationship Detection** | ❌ Manual JOINs | ✅ Automatic based on foreign keys |
| **Complexity** | Simple, lightweight | Advanced, feature-rich |
| **Dependencies** | Minimal (.NET, PostgreSQL) | Requires AI API keys or local LLM |
| **Use Case** | Agents with SQL knowledge | Agents with natural language only |
| **MCP Tools** | 2 tools | 3 tools |
| **Cost** | Free (no AI API calls) | Depends on AI provider usage |
| **Performance** | Fast (direct SQL) | Depends on AI API latency |
| **Privacy** | High (no external API calls) | Depends on AI provider (can use local Ollama/LM Studio) |

**When to use postgres-mcp**:
- Your AI agent already knows SQL
- You want minimal dependencies (no AI API keys)
- You need fast, direct database access
- You prioritize privacy (no external API calls)
- You want predictable costs (no AI API usage)

**When to use postgres-nl-mcp**:
- Your AI agent uses natural language queries
- You want AI-powered query generation and optimization
- You need automatic relationship detection
- You have AI API keys or local LLM (Ollama/LM Studio)
- You want advanced features like query explanations

## Security

### Built-in Security Features

1. **Read-Only by Design**:
   - Only SELECT queries are allowed
   - INSERT, UPDATE, DELETE, DROP blocked at multiple layers
   - No schema modifications allowed
   - Comprehensive keyword and pattern validation

2. **Query Validation**:
   - Regex-based SQL parsing
   - Dangerous keyword detection
   - Dangerous function blocking
   - Input sanitization

3. **Rate Limiting**:
   - Configurable requests per minute per IP address
   - Protects against abuse and DoS attacks
   - Can be disabled in development

4. **Query Limits**:
   - Maximum rows per query (default: 10,000)
   - Query timeout enforcement (default: 30 seconds)
   - Automatic LIMIT clause injection if missing

5. **Connection Security**:
   - SSL/TLS support for database connections
   - Connection pooling with Npgsql
   - Connection timeout enforcement
   - No credentials logged

6. **Schema Filtering**:
   - Block system schemas (pg_catalog, information_schema)
   - Configurable schema whitelist/blacklist
   - Table-level access control

### Security Best Practices

**Never commit secrets**:
```bash
# ✅ Good: Environment variables
export Postgres__DefaultConnectionString="Host=..."

# ✅ Good: User secrets (development)
dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=..."

# ✅ Good: Docker secrets (production)
docker secret create db_connection ./connection_string.txt

# ❌ Bad: Hardcode in appsettings.json
"DefaultConnectionString": "Host=...;Password=secret"  # DON'T DO THIS!
```

**Connection strings**:
```bash
# ✅ Good: SSL enabled, read-only user
Host=localhost;Database=mydb;Username=readonly_user;Password=pass;SSL Mode=Require

# ✅ Good: Separate read-only user
CREATE USER readonly_user WITH PASSWORD 'pass';
GRANT CONNECT ON DATABASE mydb TO readonly_user;
GRANT USAGE ON SCHEMA public TO readonly_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly_user;

# ❌ Bad: Superuser credentials
Username=postgres;Password=admin123  # DON'T DO THIS!
```

**Production checklist**:
- [ ] Use SSL/TLS for PostgreSQL connections (`SSL Mode=Require`)
- [ ] Enable rate limiting (`Security__EnableRateLimiting=true`)
- [ ] Set appropriate query timeout (`Security__MaxQueryExecutionSeconds=30`)
- [ ] Configure row limits (`Security__MaxRowsPerQuery=10000`)
- [ ] Use read-only database user (GRANT SELECT only)
- [ ] Store secrets securely (environment variables, key vaults)
- [ ] Review blocked schemas and tables
- [ ] Enable comprehensive logging (but don't log sensitive data)
- [ ] Set up monitoring and alerting
- [ ] Use HTTPS for API endpoints

## Testing

### Test Categories

**Unit Tests**:
- Service logic and business rules
- SQL validation and safety checks
- Configuration parsing
- Model validation

**Integration Tests**:
- Database operations (requires PostgreSQL)
- End-to-end MCP tool execution
- Query execution and result parsing

**Controller Tests**:
- API endpoint behavior
- Request validation
- Response formatting
- Error handling

### Running Tests

```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~DatabaseServiceTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~DatabaseServiceTests.ScanDatabaseStructure_ReturnsAllTables"

# By category (if using traits)
dotnet test --filter "Category=Unit"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Watch mode
dotnet watch test
```

### Writing Tests

Follow xUnit conventions and MCP-specific test patterns:

```csharp
public class QueryValidationServiceTests
{
    [Fact]
    public void ValidateQuery_SelectQuery_ReturnsTrue()
    {
        // Arrange
        var service = new QueryValidationService();
        var query = "SELECT * FROM customers";

        // Act
        var result = service.ValidateQuery(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("INSERT INTO customers VALUES (1, 'Test')")]
    [InlineData("UPDATE customers SET name = 'Test'")]
    [InlineData("DELETE FROM customers")]
    public void ValidateQuery_ModificationQuery_ReturnsFalse(string query)
    {
        // Arrange
        var service = new QueryValidationService();

        // Act
        var result = service.ValidateQuery(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("read-only", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
```

## Development Guidelines

### Adding a New MCP Tool

To add a new tool to the server:

1. **Define the tool schema** in `McpProtocolModels.cs`:
```csharp
public class Tool
{
    public string Name { get; set; } = "my_new_tool";
    public string Description { get; set; } = "What this tool does";
    public Dictionary<string, object> InputSchema { get; set; } = new()
    {
        {
            "type", "object"
        },
        {
            "properties", new Dictionary<string, object>
            {
                { "param1", new { type = "string", description = "First parameter" } }
            }
        },
        {
            "required", new[] { "param1" }
        }
    };
}
```

2. **Implement the tool logic** in a service (e.g., `DatabaseService.cs`):
```csharp
public async Task<object> ExecuteMyNewToolAsync(string param1, CancellationToken cancellationToken = default)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(param1))
        throw new ArgumentException("param1 is required", nameof(param1));

    // Implement logic
    var result = await _database.QueryAsync(param1, cancellationToken);

    return new { success = true, data = result };
}
```

3. **Register the tool** in the MCP endpoint handler:
```csharp
public async Task<JsonRpcResponse> HandleToolCallAsync(string name, Dictionary<string, object> arguments)
{
    return name switch
    {
        "my_new_tool" => await _service.ExecuteMyNewToolAsync(
            arguments.GetString("param1")),
        _ => throw new Exception($"Unknown tool: {name}")
    };
}
```

4. **Write tests** for the tool functionality
5. **Document** the tool in README.md and this CLAUDE.md

### Adding SSE Notifications

Send real-time notifications when important events occur:

```csharp
// Inject ISeNotificationService
public QueryService(ISseNotificationService sseService)
{
    _sseService = sseService;
}

// Send notification
public async Task ExecuteQueryAsync(string query)
{
    var result = await _database.ExecuteAsync(query);

    // Notify subscribers
    await _sseService.SendNotificationAsync("query_executed",
        new
        {
            query = query,
            rowsAffected = result.RowCount,
            executionTimeMs = result.ExecutionTimeMs,
            timestamp = DateTime.UtcNow
        });

    return result;
}
```

### Adding Resources

Implement `IResourceProvider` to expose database resources:

```csharp
public class CustomResourceProvider : IResourceProvider
{
    public async Task<IEnumerable<Resource>> ListResourcesAsync(CancellationToken cancellationToken)
    {
        return new[]
        {
            new Resource
            {
                Uri = "postgres://localhost:5432/tables/customers",
                Name = "customers",
                Description = "Customer records",
                MimeType = "application/json"
            }
        };
    }

    public async Task<string> ReadResourceAsync(string uri, CancellationToken cancellationToken)
    {
        if (uri == "postgres://localhost:5432/tables/customers")
        {
            var schema = await _database.GetTableSchemaAsync("customers", cancellationToken);
            return JsonSerializer.Serialize(schema);
        }

        throw new ArgumentException($"Unknown resource: {uri}");
    }

    public async Task SubscribeAsync(string uri, Func<string, Task> onUpdate, CancellationToken cancellationToken)
    {
        // Implement subscription logic (call onUpdate when resource changes)
    }
}
```

### Adding New Prompts

Add new prompt templates in `PromptProvider`:

```csharp
public async Task<Prompt> GetPromptAsync(string name, Dictionary<string, string> arguments)
{
    return name switch
    {
        "list_tables" => new Prompt
        {
            Name = "list_tables",
            Description = "List all tables in the database",
            Arguments = Array.Empty<PromptArgument>(),
            Messages = new[]
            {
                new PromptMessage
                {
                    Role = "user",
                    Content = new TextContent
                    {
                        Text = "List all tables in the public schema with their row counts and sizes."
                    }
                }
            }
        },
        "my_template" => new Prompt
        {
            Name = "my_template",
            Description = "My custom template",
            Arguments = new[]
            {
                new PromptArgument
                {
                    Name = "table_name",
                    Description = "The table to analyze",
                    Required = true
                }
            },
            Messages = new[]
            {
                new PromptMessage
                {
                    Role = "user",
                    Content = new TextContent
                    {
                        Text = $"Analyze the {arguments["table_name"]} table..."
                    }
                }
            }
        },
        _ => throw new ArgumentException($"Unknown prompt: {name}")
    };
}
```

## File Organization

The project is organized into logical concerns following .NET best practices:

**Models** (by data type):
- `JsonRpcModels.cs` - JSON-RPC 2.0 request/response structures
- `McpProtocolModels.cs` - MCP v2.0 types (Tool, Resource, Prompt, Content types)
- `DatabaseModels.cs` - Database schema models (Table, Column, Index, ForeignKey)
- `PostgresOptions.cs` - PostgreSQL connection configuration
- `SecurityOptions.cs` - Security and rate limiting settings
- `ServerConnectionOptions.cs` - Server initialization and MCP metadata

**Services** (by responsibility):
- `DatabaseService.cs` - Core database operations
- `QueryValidationService.cs` - SQL validation and security
- `ConnectionService.cs` - Connection management
- `ISseNotificationService.cs` / `SseNotificationService.cs` - Event streaming
- `IResourceProvider.cs` / `ResourceProvider.cs` - Database resources
- `IPromptProvider.cs` / `PromptProvider.cs` - Query templates

**Controllers**:
- `McpProtocolEndpoints.cs` - All HTTP endpoints (JSON-RPC, SSE, discovery)

**Supporting Files**:
- `Program.cs` - DI configuration and middleware setup
- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development overrides

## Testing MCP Compliance

### Verifying JSON-RPC 2.0 Compliance

Test that all responses follow JSON-RPC 2.0 spec:

```bash
# Test initialize
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-11-25",
      "clientInfo": {"name": "test", "version": "1.0"}
    }
  }' | jq
```

Expected response structure:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { /* response data */ }
}
```

### Verifying MCP Protocol Compliance

Test all MCP methods are implemented:

```bash
# tools/list
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# resources/list
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"resources/list"}'

# prompts/list
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"prompts/list"}'
```

### Verifying SSE Notifications

Test that SSE endpoint works and streams events:

```bash
# Subscribe to SSE
curl -N http://localhost:5000/mcp/v1/sse

# In another terminal, trigger an event
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "query_database",
      "arguments": {"connectionString": "...", "query": "SELECT 1"}
    }
  }'

# You should see notifications in the SSE stream
```

### Reference Documentation

For detailed examples and integration patterns:
- **examples/sample-requests.md** - Complete JSON-RPC 2.0 request examples
- **examples/INTEGRATION_GUIDE.md** - How to integrate with your MCP client
- **MCP Specification**: https://modelcontextprotocol.io/

## Troubleshooting

### Common Issues

**Issue**: Database connection fails
```
Error: "Connection refused" or "Authentication failed"
```
**Solutions**:
- Verify connection string format: `Host=...;Port=5432;Database=...;Username=...;Password=...`
- Check PostgreSQL is running: `docker ps` or `pg_isready`
- Test connectivity: `psql -h localhost -U postgres -d testdb`
- Verify credentials are correct
- Check firewall/network settings

**Issue**: Query validation error
```
Error: "Query is not read-only" or "Blocked keyword detected"
```
**Solutions**:
- Ensure query starts with SELECT or WITH
- Remove any INSERT, UPDATE, DELETE, DROP keywords
- Remove transaction control keywords (BEGIN, COMMIT, etc.)
- Check for blocked functions (pg_read_file, etc.)
- Review the list of blocked operations in documentation

**Issue**: Rate limit exceeded
```
HTTP 429: Too Many Requests
```
**Solutions**:
- Wait for the rate limit window to reset (1 minute)
- Adjust `Security__RequestsPerMinute` setting
- Disable rate limiting in development: `Security__EnableRateLimiting=false`
- Restart application to clear rate limit cache

**Issue**: Query timeout
```
Error: "Query execution timed out"
```
**Solutions**:
- Increase `Security__MaxQueryExecutionSeconds` value
- Optimize slow queries (add indexes, reduce data)
- Check database performance: `EXPLAIN ANALYZE <query>`
- Verify database isn't overloaded

**Issue**: Port conflicts
```
Error: "Address already in use"
```
**Solutions**:
- MCP Server uses port 5000 (HTTP) and 5001 (HTTPS)
- PostgreSQL uses port 5432
- pgAdmin uses port 8080
- Change ports in docker-compose.yml or appsettings.json
- Check what's using the port: `lsof -i :5000` (macOS/Linux) or `netstat -ano | findstr :5000` (Windows)

**Issue**: Docker build fails
```
Error: "failed to compute cache key"
```
**Solutions**:
```bash
docker-compose build --no-cache postgres-mcp
docker-compose up -d
```

**Issue**: SSE connection drops
```
Error: "Connection closed" or no events received
```
**Solutions**:
- Verify `/mcp/v1/sse` endpoint is accessible
- Check that SSE notifications are enabled: `MCP__EnableSseNotifications=true`
- Look at server logs for errors in event streaming
- Ensure client is keeping the connection open (not timing out)
- Check proxy/firewall doesn't close persistent connections

**Issue**: JSON-RPC error handling
```
Error: {"jsonrpc":"2.0","error":{"code":-32602,"message":"Invalid params"}}
```
**Solutions**:
- Verify request structure matches JSON-RPC 2.0 spec
- Check all required fields are present (jsonrpc, method, id, params)
- Validate parameter types match the method signature
- Enable debug logging: `Logging__LogLevel__PostgresMcp=Debug`

**Issue**: Resource subscription fails
```
Error: "Unknown resource" or "Resource not found"
```
**Solutions**:
- Check resource URI format: `postgres://host:port/type/name`
- Verify resource exists via `resources/list`
- Ensure ResourceProvider is properly registered in DI
- Check logs for resource resolution errors

**Issue**: Prompt argument substitution not working
```
Error: "Unknown argument" or prompt template has unsubstituted variables
```
**Solutions**:
- Verify argument names match prompt definition
- Check required arguments are provided
- Ensure arguments are passed as dictionary in prompts/get
- Review PromptProvider implementation for argument handling

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "PostgresMcp": "Trace",
      "Microsoft.AspNetCore": "Warning"
    },
    "LogQueries": true,
    "LogResults": false  // Be careful with sensitive data
  }
}
```

Or via environment variables:

```bash
export Logging__LogLevel__Default=Debug
export Logging__LogQueries=true
export ASPNETCORE_ENVIRONMENT=Development
```

### Testing Endpoints

```bash
# Health check
curl http://localhost:5000/health

# List tools
curl http://localhost:5000/mcp/tools | jq

# Test scan_database_structure
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "scan_database_structure",
    "arguments": {
      "connectionString": "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password",
      "schemaFilter": "public"
    }
  }' | jq

# Test query_database
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database",
    "arguments": {
      "connectionString": "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password",
      "query": "SELECT * FROM customers LIMIT 10"
    }
  }' | jq
```

## Working with Claude

### When Adding Features

1. **Understand MCP v2.0 architecture**: Review the MCP Protocol Implementation section
2. **Understand the read-only constraint**: This server must NEVER allow write operations
3. **Read the code first**: Use the Read tool to understand existing implementation
4. **Follow .NET 10 conventions**: Use modern C# 13 features (records, init-only, etc.)
5. **Maintain JSON-RPC 2.0 compliance**: All responses must follow JSON-RPC 2.0 spec
6. **Update tests**: Add xUnit tests for new functionality
7. **Update validation**: Ensure new features maintain read-only security
8. **Test MCP compliance**: Verify with JSON-RPC 2.0 and MCP v2.0 test examples
9. **Update documentation**: Update both README.md and this CLAUDE.md
10. **Test thoroughly**: Run tests, build, and test locally with Docker Compose

### When Debugging

1. **Check logs**: Look at Serilog output in console or log files
2. **Verify configuration**: Check user secrets, environment variables, appsettings.json
3. **Test endpoints**: Use curl with JSON-RPC 2.0 format or the Scalar UI
4. **Review JSON-RPC compliance**: Check request/response structure
5. **Review MCP compliance**: Verify methods and parameters match spec
6. **Test query validation**: Check QueryValidationService for blocked patterns
7. **Review code**: Check for common .NET issues (null references, async/await, disposal)
8. **Enable debug mode**: Set `ASPNETCORE_ENVIRONMENT=Development` and logging to Debug
9. **Test SSE**: Use `curl -N` to verify SSE events are streaming
10. **Check resources/prompts**: Verify ResourceProvider and PromptProvider are wired correctly

### When Refactoring

1. **Maintain read-only security**: Never compromise on query validation
2. **Maintain MCP compliance**: Don't break JSON-RPC 2.0 or MCP protocol compliance
3. **Maintain API compatibility**: Existing tools/resources/prompts must work unchanged
4. **Update tests**: Ensure all tests pass after changes
5. **Add JSON-RPC tests**: Verify compliance with JSON-RPC 2.0 spec
6. **Follow patterns**: Maintain consistency with ASP.NET Core best practices
7. **Document changes**: Update XML documentation comments
8. **Update CLAUDE.md**: Reflect architectural changes

## Code Quality Standards

### .NET and C# Standards

- Use .NET 10 and C# 13 features (record types, required keyword, init-only properties, etc.)
- Target `net10.0` minimum in project file
- Follow .NET naming conventions (PascalCase for public, camelCase for private)
- Implement dependency injection via built-in DI container
- Use async/await throughout (all I/O operations should be async)
- Add XML documentation comments for public APIs with `<summary>`, `<param>`, `<returns>`, `<exception>`
- Implement IDisposable/IAsyncDisposable when managing resources
- Use nullable reference types (`#nullable enable`) and handle nullability properly
- Use records for immutable data structures (DTO models)
- Use init-only properties for configuration objects

### Code Organization

- One class per file
- Use namespaces matching folder structure
- Separate concerns (controllers, services, models)
- Use interfaces for abstraction
- Keep controllers thin, move logic to services
- Use strongly-typed configuration (IOptions<T>)

### Security-First Development

- **Always validate queries**: Every query must pass through QueryValidationService
- **Multiple validation layers**: Regex, keyword detection, schema filtering
- **Test security**: Write tests for blocked operations
- **Document security**: Explain why certain operations are blocked
- **Never trust input**: Validate all user-provided connection strings and queries

### JSON-RPC 2.0 Compliance

All requests/responses must follow JSON-RPC 2.0 specification:
- Every response includes `jsonrpc: "2.0"` field
- Request ID in response must match request ID (even for errors)
- Errors must use standard error codes (-32700 to -32603, -32000 to -32099)
- Error responses have `error` field, not `result` field
- Success responses have `result` field, not `error` field
- Never mix `result` and `error` in same response
- Notifications (no `id`) should not receive responses

### MCP Protocol v2.0 Compliance

- Implement all required MCP methods (initialize, tools/list, tools/call, resources/list, prompts/list)
- Use correct MCP type definitions for Tools, Resources, Prompts
- SSE events must follow MCP event format
- Resource URIs must follow `postgres://host:port/type/resource` format
- Prompt argument substitution must use correct placeholder syntax
- Server capabilities must be accurately reported in initialize response

### Testing

- Write unit tests for all services
- Use xUnit framework
- Follow AAA pattern (Arrange, Act, Assert)
- Use [Fact] for single tests, [Theory] for parameterized tests
- Mock external dependencies (databases in unit tests, not integration tests)
- Test both success and failure paths
- Test all blocked operations to ensure they fail
- Aim for >80% code coverage

### Documentation

- Add XML documentation comments (///) for public APIs
- Include <summary>, <param>, <returns>, <exception>
- Document security constraints
- Document non-obvious logic with inline comments
- Keep README.md and CLAUDE.md up to date
- Document configuration options

## Performance Considerations

- Use async/await for all I/O operations
- Implement connection pooling (Npgsql handles this automatically)
- Use efficient queries with proper indexes
- Stream large result sets when possible
- Monitor database query performance
- Profile and optimize hot paths
- Consider caching schema information (future enhancement)

## Examples and Documentation

This project includes comprehensive examples and documentation for integrating with MCP clients:

**Examples Directory** (`examples/`):
- **`sample-requests.md`** - Complete collection of all JSON-RPC 2.0 requests demonstrating:
  - Initialize and handshake
  - Tool discovery and execution
  - Resource listing and subscription
  - Prompt retrieval with arguments
  - Error handling examples

- **`INTEGRATION_GUIDE.md`** - Step-by-step guide for:
  - Setting up MCP client connection
  - Handling SSE notifications
  - Error recovery and retry logic
  - Performance optimization tips
  - Testing checklist

**VS Code Integration** (`.vscode/`):
- **`mcp-settings.json`** - MCP server configuration for VS Code
- **`launch.json`** - Debug configuration for stepping through code
- **`tasks.json`** - Build, test, and run tasks

**Main Documentation**:
- **`README.md`** - User-facing documentation (getting started, configuration, usage)
- **`CLAUDE.md`** - This file (technical context for Claude and developers)

**Reference**:
- **MCP Protocol Specification**: https://modelcontextprotocol.io/
- **JSON-RPC 2.0 Specification**: https://www.jsonrpc.org/specification

## Future Enhancements

- [ ] Schema caching for faster structure scans (v2.1)
- [ ] Query result caching with TTL (v2.1)
- [ ] Support for parameterized queries (v2.2)
- [ ] Query history and auditing (v2.2)
- [ ] Connection string encryption at rest (v2.3)
- [ ] Multi-database support (multiple connections) (v3.0)
- [ ] Query cost estimation before execution (v2.2)
- [ ] Real-time query monitoring and metrics (v2.2)
- [ ] Advanced resource subscriptions with filtering (v2.1)
- [ ] Export formats (CSV, JSON, Excel) (v2.2)
- [ ] Authentication and authorization (v3.0)
- [ ] WebSocket support for streaming results (v3.0)
- [ ] GraphQL endpoint for queries (v3.0)
- [ ] Relationship visualization (v2.2)
- [ ] Query plan analysis and optimization hints (v2.2)

## Related Documentation

- **Main Repository Context**: `/CLAUDE.md` - Repository-wide guidelines and patterns
- **User Documentation**: `README.md` - User-facing documentation
- **Configuration Files**: `appsettings.json`, `appsettings.Development.json`
- **Docker Setup**: `docker-compose.yml`, `Dockerfile`
- **Related Project**: `/mcp-servers/postgres-nl-mcp/CLAUDE.md` - AI-powered variant

## Questions for Claude

When working on the PostgreSQL MCP Server, you can ask:

### MCP Protocol v2.0 Questions
- "How does JSON-RPC 2.0 work in this server?"
- "What's the difference between JSON-RPC requests and MCP methods?"
- "How do I implement a new MCP method?"
- "How do SSE notifications work?"
- "How do I subscribe to resource updates?"
- "How do prompts work with argument substitution?"
- "What are the required MCP server capabilities?"
- "How do I test MCP protocol compliance?"

### Architecture and Implementation
- "How does query validation work?"
- "What operations are blocked and why?"
- "How do I add a new validation rule?"
- "How does the scan_database_structure tool work?"
- "How can I add a new MCP tool?"
- "How do I implement a custom ResourceProvider?"
- "How do I add new prompts?"
- "How does the ConnectionService handle connection pooling?"

### Configuration and Deployment
- "How do I configure the server?"
- "Where should I add a new configuration option?"
- "How do I set up the MCP server for development?"
- "What environment variables are available?"
- "How do I deploy to Docker?"
- "How do I enable SSL/TLS?"

### Security and Best Practices
- "How does rate limiting work?"
- "How do I ensure a new feature maintains read-only security?"
- "What schemas and tables should I block?"
- "How do I prevent SQL injection?"
- "What's the difference between postgres-mcp and postgres-nl-mcp?"
- "How do I implement authentication?"

### Testing and Debugging
- "How do I test with a local database?"
- "How do I write tests for MCP methods?"
- "How do I debug JSON-RPC requests?"
- "How do I verify SSE notifications are working?"
- "How do I test query validation?"
- "How do I check logs in development mode?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for the PostgreSQL MCP Server v2.0 project.
