# PostgreSQL MCP Server

A **read-only** Model Context Protocol (MCP) server for PostgreSQL database operations. This MCP server provides secure, read-only access to PostgreSQL databases for AI agents, blocking all data and schema modifications.

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

## Quick Start

### Using Docker Compose (Recommended)

1. **Start all services**:
   ```bash
   cd mcp-servers/postgres-mcp
   docker-compose up -d
   ```

2. **Access the services**:
   - **MCP Server API**: http://localhost:5000
   - **API Documentation**: http://localhost:5000/scalar/v1
   - **PostgreSQL**: localhost:5432 (postgres/password)
   - **pgAdmin**: http://localhost:8080 (admin@admin.com/admin)

3. **Test the API**:
   ```bash
   curl http://localhost:5000/mcp/tools
   ```

### Local Development

1. **Prerequisites**:
   - .NET 9 SDK
   - PostgreSQL 16+

2. **Configure the application**:
   ```bash
   cd src/PostgresMcp
   dotnet user-secrets init
   dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;Database=testdb;Username=postgres;Password=yourpass"
   ```

3. **Run the server**:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

4. **Run tests**:
   ```bash
   cd ../../
   dotnet test
   ```

## Configuration

### Environment Variables

Configure via environment variables (recommended for production):

```bash
# PostgreSQL Configuration
Postgres__DefaultConnectionString="Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass;SSL Mode=Require"
Postgres__MaxRetries=3
Postgres__ConnectionTimeoutSeconds=30
Postgres__CommandTimeoutSeconds=60

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
    "DefaultConnectionString": null,
    "MaxRetries": 3,
    "UseSsl": true
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

## API Usage

### List Available Tools

```bash
GET /mcp/tools
```

**Response**:
```json
{
  "tools": [
    {
      "name": "scan_database_structure",
      "description": "Scan and analyze PostgreSQL database structure...",
      "inputSchema": { ... }
    },
    {
      "name": "query_database",
      "description": "Execute a read-only SELECT query...",
      "inputSchema": { ... }
    }
  ]
}
```

### Call a Tool

```bash
POST /mcp/tools/call
Content-Type: application/json

{
  "name": "scan_database_structure",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass"
  }
}
```

### Query Database

```bash
POST /mcp/tools/call
Content-Type: application/json

{
  "name": "query_database",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass",
    "query": "SELECT * FROM customers LIMIT 10"
  }
}
```

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
│   │   ├── Controllers/          # MCP API controllers
│   │   ├── Models/               # Data models
│   │   ├── Services/             # Business logic
│   │   ├── Program.cs            # App entry point
│   │   └── appsettings.json      # Configuration
│   └── PostgresMcp.Tests/        # Unit tests
├── docker-init/                  # Database init scripts
├── Dockerfile                    # Docker build
├── docker-compose.yml            # Docker orchestration
└── README.md                     # This file
```

### Technology Stack

- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Npgsql** - PostgreSQL data provider
- **Serilog** - Structured logging
- **Scalar** - OpenAPI documentation
- **AspNetCoreRateLimit** - Rate limiting

## Comparison with postgres-nl-mcp

| Feature | postgres-mcp | postgres-nl-mcp |
|---------|--------------|-----------------|
| **Purpose** | Direct read-only database access | AI-powered natural language queries |
| **AI/LLM** | ❌ None | ✅ Multiple providers (OpenAI, Claude, Gemini, Ollama, LM Studio) |
| **Query Generation** | ❌ Manual SQL only | ✅ Natural language to SQL |
| **Query Optimization** | ❌ None | ✅ AI-powered optimization |
| **Complexity** | Simple, lightweight | Advanced, feature-rich |
| **Use Case** | Agents with SQL knowledge | Agents with natural language only |
| **Dependencies** | Minimal | LLM API keys required |

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

1. **Connection failures**:
   - Verify connection string format
   - Check if PostgreSQL is running
   - Ensure network connectivity
   - Verify credentials

2. **Query validation errors**:
   - Ensure query starts with SELECT or WITH
   - Remove any data modification keywords
   - Remove any schema modification keywords
   - Check for blocked functions

3. **Rate limiting**:
   - Disable in development: `Security__EnableRateLimiting=false`
   - Adjust limit: `Security__RequestsPerMinute=120`

4. **Port conflicts**:
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
