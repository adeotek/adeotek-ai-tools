# PostgreSQL MCP Server

A **read-only** Model Context Protocol (MCP) server for PostgreSQL database operations. This MCP server provides secure, read-only access to PostgreSQL databases for AI agents and applications, blocking all data and schema modifications.

**Version**: 2.0.0 | **MCP Protocol**: 2025-11-25 | **.NET**: 10

## Features

### 🔒 Read-Only by Design
- **Blocks data modifications**: INSERT, UPDATE, DELETE, TRUNCATE, MERGE are prohibited
- **Blocks schema changes**: CREATE, ALTER, DROP, RENAME, GRANT, REVOKE are prohibited
- **Blocks dangerous operations**: System functions, procedural code, transactions, locks
- **Comprehensive validation**: Multiple layers of security to ensure read-only access

### 🔍 Tool 1: scan_database_structure
Scan and analyze PostgreSQL database structure in detail.

**Capabilities**:
- List all tables with their schemas
- Display columns with types, constraints, defaults
- Show primary keys and foreign keys
- List indexes (unique, non-unique)
- Identify table relationships
- Provide row count estimates

**Example Use Cases**:
- "What tables exist in the database?"
- "Show me the structure of the customers table"
- "What foreign keys reference the orders table?"

### 💬 Tool 2: query_database
Execute read-only SELECT queries against the database.

**Capabilities**:
- Execute SELECT queries only
- Automatic safety validation
- Row limit enforcement (configurable)
- Query timeout protection
- Execution time tracking
- Result truncation for large datasets

**Example Use Cases**:
- "SELECT * FROM customers WHERE created_at > '2024-01-01'"
- "Show me the top 10 orders by total amount"
- "List all products in the Electronics category"

### ✅ Security Features
- Query validation (regex-based SQL parsing)
- Schema filtering (block system schemas)
- Rate limiting (configurable)
- Query execution timeout
- Max rows per query limit
- Dangerous function blocking
- No AI/LLM dependencies (simple, lightweight)

## What's New in v2.0

### Full MCP Protocol Support (v2025-11-25)
- **JSON-RPC 2.0 Endpoint**: Standard MCP clients connect via `/mcp/v1/messages`
- **All MCP Methods**: initialize, initialized, ping, tools/*, resources/*, prompts/*
- **Batch Requests**: Send multiple requests in a single batch operation
- **Error Codes**: Proper JSON-RPC error codes (-32700, -32600, -32601, -32602, -32603, -32000-32002)
- **Tasks Support**: Async tasks capability declared (planned for future release - currently returns not-implemented for task operations)

### Real-Time Notifications (SSE)
- **Server-Sent Events**: Real-time updates via `/mcp/v1/sse` endpoint
- **Heartbeat Support**: Automatic connection keepalive
- **Event Types**: resources/updated, tools/list_changed, prompts/list_changed, progress
- **Subscribe/Unsubscribe**: Dynamic notification subscriptions

### Resources System
- **List Resources**: Available PostgreSQL databases, connections, and metadata
- **Read Resources**: Access resource contents with detailed information
- **Pagination**: Handle large result sets efficiently
- **Subscribe to Changes**: Get notified when resources are updated

### Prompts System
- **4 Built-in Templates**: analyze_table, find_relationships, recent_data, search_columns
- **Argument Substitution**: Dynamic parameter replacement for flexible prompts
- **AI Agent Support**: Helpful for AI agents to interact with your database
- **Easy Integration**: Simple interface for prompt discovery and execution

### Server Discovery
- **`.well-known/mcp.json`**: Standard MCP server discovery endpoint
- **Automatic Detection**: MCP clients can discover your server capabilities
- **Protocol Negotiation**: Proper version and capability advertisement

## Quick Start

### Using Docker Compose (Recommended)

1. **Start all services**:
   ```bash
   cd mcp-servers/postgres-mcp
   docker-compose up -d
   ```

2. **Access the services**:
   - **MCP Server API**: http://localhost:5000
   - **MCP Protocol Endpoint**: http://localhost:5000/mcp/v1/messages (JSON-RPC 2.0)
   - **API Documentation**: http://localhost:5000/scalar/v1
   - **Server Discovery**: http://localhost:5000/.well-known/mcp.json
   - **PostgreSQL**: localhost:5432 (postgres/password)
   - **pgAdmin**: http://localhost:8080 (admin@admin.com/admin)

3. **Connect with Standard MCP Client** (recommended for v2.0):
   ```bash
   # Using an MCP-compatible client, connect to:
   ws://localhost:5000/mcp/v1/messages
   # or
   http://localhost:5000/mcp/v1/messages (for HTTP transport)
   ```

4. **Test the MCP JSON-RPC Endpoint**:
   ```bash
   # List available tools using MCP protocol
   curl -X POST http://localhost:5000/mcp/v1/messages \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "id": 1,
       "method": "tools/list",
       "params": {}
     }'
   ```

5. **Subscribe to Real-Time Notifications** (SSE):
   ```bash
   # Connect to real-time event stream
   curl http://localhost:5000/mcp/v1/sse
   ```

### Local Development

1. **Prerequisites**:
   - .NET 10 SDK
   - PostgreSQL 16+

2. **Run the server**:
   ```bash
   cd src/PostgresMcp
   dotnet restore
   dotnet build
   dotnet run
   ```

3. **Test the MCP v2.0 Endpoint**:
   ```bash
   # Using standard MCP JSON-RPC 2.0 protocol
   curl -X POST http://localhost:5000/mcp/v1/messages \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "id": 1,
       "method": "tools/list",
       "params": {}
     }'
   ```

4. **Monitor Real-Time Events**:
   ```bash
   # Connect to SSE endpoint for notifications
   curl http://localhost:5000/mcp/v1/sse
   ```

5. **Run tests**:
   ```bash
   cd ../../
   dotnet test
   ```

6. **Review Example Requests**:
   - See `sample-requests.md` for detailed API examples
   - See `INTEGRATION_GUIDE.md` for MCP client integration instructions

## Configuration

### PostgreSQL Connection

The MCP server supports two methods for configuring PostgreSQL connections:

#### Method 1: Pre-configured Connection String (Recommended for Simple Deployments)

Configure a base connection string in `appsettings.json` or via environment variables. This allows the server to connect to a PostgreSQL instance automatically on startup.

**Via environment variable**:
```bash
Postgres__ConnectionString=Host=localhost;Port=5432;Username=postgres;Password=yourpass
```

**Via appsettings.json**:
```json
{
  "Postgres": {
    "ConnectionString": "Host=localhost;Port=5432;Username=postgres;Password=yourpass"
  }
}
```

**Note**: The connection string should specify the PostgreSQL server but can omit the database parameter. This allows tools to connect to any database on the server.

**Example tool call** with pre-configured connection:
```json
{
  "name": "scan_database_structure",
  "arguments": {
    "database": "testdb"
  }
}
```

#### Method 2: Runtime Initialization (For Dynamic Configuration)

Configure PostgreSQL server connection parameters via the initialization endpoint at runtime.

**Initialize the server**:
```bash
POST /mcp/initialize
Content-Type: application/json

{
  "host": "localhost",
  "port": 5432,
  "username": "postgres",
  "password": "yourpass"
}
```

**Note**: Runtime initialization overrides any pre-configured connection string.

**Database per tool call**: Each MCP tool call accepts a `database` parameter to specify which database to query.

**Example**:
```json
{
  "name": "query_database",
  "arguments": {
    "database": "mydb",
    "query": "SELECT * FROM customers LIMIT 10"
  }
}
```

### Environment Variables

Configure connection pool and security settings via environment variables (recommended for production):

```bash
# PostgreSQL Connection String (optional - can also configure at runtime)
Postgres__ConnectionString=Host=localhost;Port=5432;Username=postgres;Password=yourpass

# PostgreSQL Connection Pool Settings
Postgres__MaxRetries=3
Postgres__ConnectionTimeoutSeconds=30
Postgres__CommandTimeoutSeconds=60
Postgres__UseSsl=true
Postgres__MaxPoolSize=100
Postgres__MinPoolSize=0

# Security Settings
Security__EnableRateLimiting=true
Security__RequestsPerMinute=60
Security__MaxRowsPerQuery=10000
Security__MaxQueryExecutionSeconds=30
```

### Configuration Files

**appsettings.json** - Default settings:
```json
{
  "Postgres": {
    "ConnectionString": null,
    "MaxRetries": 3,
    "ConnectionTimeoutSeconds": 30,
    "CommandTimeoutSeconds": 60,
    "UseSsl": true,
    "MaxPoolSize": 100,
    "MinPoolSize": 0
  },
  "Security": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "MaxRowsPerQuery": 10000,
    "MaxQueryExecutionSeconds": 30
  }
}
```

**appsettings.Development.json** - Development overrides:
```json
{
  "Security": {
    "EnableRateLimiting": false,
    "MaxRowsPerQuery": 1000
  }
}
```

## API Endpoints

### MCP v2.0 - JSON-RPC 2.0 Protocol (Recommended)

All v2.0 endpoints use the JSON-RPC 2.0 specification for standard MCP client compatibility.

#### `/mcp/v1/messages` - Primary MCP Endpoint

Handles all MCP protocol requests using JSON-RPC 2.0 format.

**Example: List Tools**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "scan_database_structure",
        "description": "Scan and analyze PostgreSQL database structure...",
        "inputSchema": {
          "type": "object",
          "properties": {
            "database": { "type": "string" }
          },
          "required": ["database"]
        }
      },
      {
        "name": "query_database",
        "description": "Execute a read-only SELECT query...",
        "inputSchema": {
          "type": "object",
          "properties": {
            "database": { "type": "string" },
            "query": { "type": "string" }
          },
          "required": ["database", "query"]
        }
      }
    ]
  }
}
```

**Example: Call a Tool**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "query_database",
    "arguments": {
      "database": "testdb",
      "query": "SELECT * FROM customers LIMIT 10"
    }
  }
}
```

#### `/mcp/v1/sse` - Real-Time Notifications

Server-Sent Events endpoint for real-time updates and notifications.

**Features**:
- Automatic heartbeat (keeps connection alive)
- Resource updates: `resources/updated`
- Tool changes: `tools/list_changed`
- Prompt updates: `prompts/list_changed`
- Progress notifications: `progress`

**Connect**:
```bash
curl http://localhost:5000/mcp/v1/sse
```

#### `/.well-known/mcp.json` - Server Discovery

Standard MCP server discovery endpoint.

**Response**:
```json
{
  "protocolVersion": "2025-11-25",
  "capabilities": {
    "tools": {},
    "resources": {},
    "prompts": {},
    "notifications": {}
  },
  "serverInfo": {
    "name": "PostgreSQL MCP Server",
    "version": "2.0.0"
  }
}
```

#### Resources - Database Resources

**List Resources**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "resources/list",
  "params": {
    "cursor": "0"
  }
}
```

**Read Resource**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read",
  "params": {
    "uri": "postgres://databases/testdb"
  }
}
```

#### Prompts - Database Query Templates

**List Prompts**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "prompts/list",
  "params": {}
}
```

**Get Prompt**:
```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "prompts/get",
  "params": {
    "name": "analyze_table",
    "arguments": {
      "table_name": "customers"
    }
  }
}
```

### Legacy API Endpoints (v1 - Backward Compatible)

These endpoints are maintained for backward compatibility. For new integrations, use the MCP v2.0 endpoints above.

#### `POST /mcp/initialize` - Initialize Server

Configure the PostgreSQL connection parameters.

```bash
POST /mcp/initialize
Content-Type: application/json

{
  "host": "localhost",
  "port": 5432,
  "username": "postgres",
  "password": "yourpass"
}
```

**Response**:
```json
{
  "success": true,
  "message": "MCP server initialized successfully",
  "configuration": {
    "host": "localhost",
    "port": 5432,
    "username": "postgres"
  }
}
```

#### `GET /mcp/configuration` - Check Status

```bash
GET /mcp/configuration
```

**Response**:
```json
{
  "configured": true,
  "host": "localhost",
  "port": 5432,
  "username": "postgres"
}
```

#### `GET /mcp/tools` - List Tools

```bash
GET /mcp/tools
```

**Response**: (Same structure as v2.0 `tools/list` result)

#### `POST /mcp/tools/call` - Execute Tool

```bash
POST /mcp/tools/call
Content-Type: application/json

{
  "name": "query_database",
  "arguments": {
    "database": "testdb",
    "query": "SELECT * FROM customers LIMIT 10"
  }
}
```

## Built-In Prompts

The server includes 4 pre-configured prompt templates to help AI agents query your database:

### 1. analyze_table
Analyzes the structure and content of a specific table.

**Arguments**:
- `table_name` (required): Name of the table to analyze

**Use Case**: Get a comprehensive overview of a table's schema and data characteristics.

### 2. find_relationships
Discovers foreign key relationships and table dependencies.

**Arguments**:
- `table_name` (required): Table to find relationships for

**Use Case**: Understand how tables are connected and what data dependencies exist.

### 3. recent_data
Retrieves recent data from a table with sorting and limits.

**Arguments**:
- `table_name` (required): Table to query
- `limit` (optional): Number of rows (default: 10)

**Use Case**: Get a quick sample of the most recent data in a table.

### 4. search_columns
Searches for columns across all tables by name pattern.

**Arguments**:
- `column_pattern` (required): Search pattern for column names
- `limit` (optional): Maximum results (default: 50)

**Use Case**: Find all columns matching a pattern across the entire database.

## Blocked Operations

The MCP server blocks the following operations to maintain read-only access:

### Data Modifications
- `INSERT` - Adding new data
- `UPDATE` - Modifying existing data
- `DELETE` - Removing data
- `TRUNCATE` - Clearing tables
- `MERGE`/`UPSERT` - Insert or update operations

### Schema Modifications
- `CREATE` - Creating tables, indexes, etc.
- `ALTER` - Modifying table structure
- `DROP` - Deleting database objects
- `RENAME` - Renaming database objects
- `GRANT`/`REVOKE` - Permission changes
- `COMMENT ON` - Metadata changes

### System Operations
- Transaction control (`BEGIN`, `COMMIT`, `ROLLBACK`)
- Lock statements (`LOCK TABLE`)
- Maintenance commands (`VACUUM`, `ANALYZE`, `REINDEX`)
- Configuration changes (`SET`)
- Messaging (`LISTEN`, `NOTIFY`)
- Dangerous functions (`pg_read_file`, `pg_execute`, etc.)
- Procedural code execution (`DO`, `$$`)

## Architecture

### Project Structure

```
postgres-mcp/
├── src/
│   ├── PostgresMcp/              # Main application
│   │   ├── Controllers/          # MCP API controllers (legacy + v2.0)
│   │   ├── Models/               # Data models
│   │   │   ├── McpModels/        # MCP protocol models (JSON-RPC 2.0)
│   │   │   ├── DatabaseModels/   # Database schema models
│   │   │   └── ResourceModels/   # Resource definitions
│   │   ├── Services/             # Business logic
│   │   │   ├── DatabaseService/  # Database operations
│   │   │   ├── QueryValidationService/  # SQL validation
│   │   │   ├── ResourcesProvider/       # Resource listing/reading
│   │   │   ├── PromptsProvider/        # Prompt templates
│   │   │   └── NotificationService/    # SSE notifications
│   │   ├── Program.cs            # App entry point
│   │   ├── appsettings.json      # Configuration
│   │   └── appsettings.Development.json
│   └── PostgresMcp.Tests/        # xUnit test suite
├── docker-init/                  # Database init scripts
├── Dockerfile                    # Docker build
├── docker-compose.yml            # Docker orchestration
├── sample-requests.md            # Example API calls
├── INTEGRATION_GUIDE.md           # Integration instructions
└── README.md                     # This file
```

### Technology Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core** - Web API framework with full MCP protocol support
- **Npgsql 8.0+** - PostgreSQL data provider
- **Serilog** - Structured logging
- **Scalar** - OpenAPI documentation UI
- **AspNetCoreRateLimit** - Rate limiting
- **JSON-RPC 2.0** - Standard MCP protocol compliance

## Comparison with postgres-nl-mcp

| Feature | postgres-mcp | postgres-nl-mcp |
|---------|--------------|-----------------|
| **Purpose** | Direct read-only database access | AI-powered natural language queries |
| **MCP Protocol Version** | ✅ v2025-11-25 (JSON-RPC 2.0) | ✅ v2025-11-25 (JSON-RPC 2.0) |
| **Resources Support** | ✅ Yes (database resources) | ✅ Yes (enhanced) |
| **Prompts Support** | ✅ 4 built-in templates | ✅ 6 advanced templates |
| **SSE Notifications** | ✅ Yes | ✅ Yes |
| **AI/LLM** | ❌ None | ✅ Multiple providers (OpenAI, Claude, Gemini, Ollama, LM Studio) |
| **Query Generation** | ❌ Manual SQL only | ✅ Natural language to SQL |
| **Query Optimization** | ❌ None | ✅ AI-powered optimization |
| **Complexity** | Simple, lightweight | Advanced, feature-rich |
| **Use Case** | Agents with SQL knowledge | Agents with natural language only |
| **Dependencies** | Minimal | LLM API keys required |

## Advanced Features

### Real-Time Notifications (SSE)

The server sends real-time notifications for important events:

```bash
# Connect to the SSE endpoint
curl http://localhost:5000/mcp/v1/sse
```

**Events emitted**:
- `resources/updated` - When database resources change
- `tools/list_changed` - When available tools change
- `prompts/list_changed` - When prompts are updated
- `progress` - For long-running operations

### Resource Discovery

Clients can discover available database resources:

```bash
POST /mcp/v1/messages
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/list",
  "params": {}
}
```

**Response includes**:
- Available PostgreSQL databases
- Connection information
- Schema details
- Table metadata

### Server Discovery (`.well-known/mcp.json`)

Standard MCP clients can auto-discover your server:

```bash
curl http://localhost:5000/.well-known/mcp.json
```

The response advertises:
- Protocol version (2025-11-25)
- Supported capabilities (tools, resources, prompts)
- Server information and version

### Batch Requests

Submit multiple requests in a single batch:

```bash
POST /mcp/v1/messages
Content-Type: application/json

[
  {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  },
  {
    "jsonrpc": "2.0",
    "id": 2,
    "method": "resources/list",
    "params": {}
  }
]
```

### Error Handling

The server returns proper JSON-RPC 2.0 error codes:

| Code | Meaning |
|------|---------|
| -32700 | Parse error |
| -32600 | Invalid request |
| -32601 | Method not found |
| -32602 | Invalid parameters |
| -32603 | Internal error |
| -32000 to -32099 | Server errors |

## Deployment

### Docker (Recommended)

```bash
cd mcp-servers/postgres-mcp
docker-compose up -d
```

### Kubernetes

Example deployment:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-mcp
spec:
  replicas: 2
  selector:
    matchLabels:
      app: postgres-mcp
  template:
    metadata:
      labels:
        app: postgres-mcp
    spec:
      containers:
      - name: postgres-mcp
        image: adeotek/postgres-mcp:latest
        ports:
        - containerPort: 5000
        env:
        - name: Postgres__DefaultConnectionString
          valueFrom:
            secretKeyRef:
              name: db-secrets
              key: connection-string
```

## Troubleshooting

### Common Issues

1. **MCP Protocol errors** (JSON-RPC 2.0):
   - Verify you're sending valid JSON-RPC 2.0 requests
   - Check that `jsonrpc: "2.0"` is included
   - Ensure the `id` field is present for request tracking
   - Check the method name (e.g., `tools/list`, `tools/call`)
   - Review error codes in the error response

2. **Connection failures**:
   - Verify connection string format
   - Check if PostgreSQL is running
   - Ensure network connectivity
   - Verify credentials
   - Test with: `curl http://localhost:5000/health`

3. **Query validation errors**:
   - Ensure query starts with SELECT or WITH
   - Remove any data modification keywords
   - Remove any schema modification keywords
   - Check for blocked functions

4. **SSE connection issues**:
   - Verify SSE endpoint: `curl http://localhost:5000/mcp/v1/sse`
   - Check that your client supports Server-Sent Events
   - Ensure no firewall is blocking connections
   - Look for connection timeout issues

5. **Server discovery not working**:
   - Test endpoint: `curl http://localhost:5000/.well-known/mcp.json`
   - Verify server is running and healthy
   - Check that CORS is not blocking requests (if cross-origin)

6. **Rate limiting**:
   - Disable in development: `Security__EnableRateLimiting=false`
   - Adjust limit: `Security__RequestsPerMinute=120`

7. **Port conflicts**:
   - MCP Server uses port 5000
   - PostgreSQL uses port 5432
   - pgAdmin uses port 8080
   - Change ports in docker-compose.yml if needed

### Debug Mode

Enable debug logging:
```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
export Logging__LogQueries=true
```

## Security Best Practices

1. **Use connection pooling** - Already configured in PostgresOptions
2. **Set query timeouts** - Prevent long-running queries
3. **Enable rate limiting** - Protect against abuse
4. **Use SSL/TLS** - Encrypt database connections
5. **Restrict schemas** - Block access to system schemas
6. **Monitor logs** - Track query patterns and errors
7. **Use secrets** - Never commit credentials to git

## Contributing

When contributing to this MCP server:

1. Maintain read-only nature - never allow write operations
2. Add comprehensive query validation
3. Include unit tests for new features
4. Update documentation
5. Follow .NET coding conventions

## License

MIT License - see root repository LICENSE file

## Related Projects

- **postgres-nl-mcp**: AI-powered natural language PostgreSQL MCP server with multiple LLM providers
- **http-agent**: Intelligent HTTP request agent with AI analysis

## Support

For issues, questions, or contributions, please visit:
https://github.com/adeotek/adeotek-ai-tools
