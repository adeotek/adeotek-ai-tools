# Adeotek SQL .NET MCP Server

A production-ready Model Context Protocol (MCP) server providing read-only access to SQL databases (Microsoft SQL Server and PostgreSQL) with comprehensive security safeguards.

**Version**: 1.0.0
**MCP Protocol**: 2025-11-25 (stdio transport)
**Technology**: .NET 10, C# 13

## Features

✅ **Multi-Database Support**: Microsoft SQL Server and PostgreSQL
✅ **5 MCP Tools**: Database listing, table listing, schema analysis, querying, execution plans
✅ **3 MCP Prompts**: Schema analysis, query assistance, performance review
✅ **Stdio Transport**: Standard MCP 2025-11-25 protocol
✅ **Read-Only by Design**: Multi-layer security validation
✅ **Comprehensive Security**: SQL injection prevention, query validation, row limits
✅ **>80% Test Coverage**: Comprehensive unit and integration tests
✅ **Docker Support**: Production-ready containerization

## Quick Start

### Using Docker (Recommended)

```bash
# Start all services (MCP server + test databases)
docker-compose up -d

# Test with PostgreSQL
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"sql_list_databases","arguments":{"connectionString":"type=postgres;host=postgres;port=5432;user=postgres;password=postgres123;database=postgres"}}}' | docker exec -i adeotek-sql-net-mcp dotnet AdeotekSqlMcp.dll

# Stop services
docker-compose down
```

### Local Development

**Prerequisites**: .NET 10 SDK

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run the server
dotnet run --project src/AdeotekSqlMcp
```

## MCP Tools

### 1. sql_list_databases

List all databases on the server.

**Input**:
```json
{
  "connectionString": "type=postgres;host=localhost;port=5432;user=postgres;password=pass;database=postgres"
}
```

**Output**: Array of databases with metadata (name, size, owner, encoding)

### 2. sql_list_tables

List all tables in a database.

**Input**:
```json
{
  "connectionString": "...",
  "database": "mydb",
  "schema": "public"
}
```

**Output**: Array of tables with metadata (schema, name, row count, size, type)

### 3. sql_describe_table

Get detailed schema information for a table.

**Input**:
```json
{
  "connectionString": "...",
  "database": "mydb",
  "schema": "public",
  "table": "customers"
}
```

**Output**: Complete table schema (columns, indexes, foreign keys, constraints)

### 4. sql_query

Execute a read-only SELECT query.

**Input**:
```json
{
  "connectionString": "...",
  "database": "mydb",
  "query": "SELECT * FROM customers WHERE created_at > '2024-01-01' LIMIT 100",
  "maxRows": 1000
}
```

**Output**: Query results with columns, rows, execution time

### 5. sql_get_query_plan

Get execution plan for a query.

**Input**:
```json
{
  "connectionString": "...",
  "database": "mydb",
  "query": "SELECT * FROM customers JOIN orders ON customers.id = orders.customer_id"
}
```

**Output**: Query execution plan (JSON for PostgreSQL, XML for SQL Server)

## MCP Prompts

### 1. analyze-schema

Analyze database schema and provide insights.

**Arguments**:
- `database` (required): Database to analyze
- `focus` (optional): Specific area (tables, relationships, indexes, etc.)

### 2. query-assistant

Help construct SQL queries from natural language.

**Arguments**:
- `database` (required): Target database
- `requirement` (required): Natural language description

### 3. performance-review

Review query performance and suggest optimizations.

**Arguments**:
- `database` (required): Database name
- `query` (required): SQL query to analyze

## Connection String Format

### PostgreSQL
```
type=postgres;host=localhost;port=5432;user=myuser;password=mypass;database=mydb;ssl=true
```

### SQL Server
```
type=mssql;host=localhost;port=1433;user=sa;password=StrongPass123;database=master;ssl=true
```

**Supported Keys**:
- `type`: "mssql" or "postgres" (required)
- `host` / `server` / `data source`: Server host (required)
- `port`: Port number (default: 5432 for PostgreSQL, 1433 for SQL Server)
- `user` / `username` / `user id` / `uid`: Database user (required)
- `password` / `pwd`: Database password (required)
- `database` / `initial catalog`: Database name (optional)
- `ssl` / `encrypt`: Enable SSL/TLS (true/false)
- `connectionTimeout` / `connect timeout`: Connection timeout in seconds
- `commandTimeout` / `request timeout`: Query timeout in seconds

## Security Features

### Read-Only Validation

**Blocked Operations**:
- Data modification: INSERT, UPDATE, DELETE, TRUNCATE, MERGE, UPSERT, REPLACE, COPY
- Schema modification: CREATE, ALTER, DROP, RENAME
- Permissions: GRANT, REVOKE
- Transaction control: BEGIN, COMMIT, ROLLBACK, SAVEPOINT
- Locking: LOCK, UNLOCK
- Maintenance: VACUUM, ANALYZE, REINDEX, CLUSTER
- Configuration: SET, RESET
- Messaging: LISTEN, NOTIFY, UNLISTEN
- Procedural code: DO, CALL, EXECUTE, DECLARE

**Dangerous Functions Blocked**:
- PostgreSQL: `pg_read_file`, `pg_read_binary_file`, `pg_execute`, `pg_terminate_backend`, `pg_sleep`
- SQL Server: `xp_cmdshell`, `sp_executesql`, `OPENROWSET`, `OPENDATASOURCE`

### Multiple Validation Layers

1. **Keyword Blocking**: Regex-based detection of dangerous keywords
2. **Function Blocking**: Prevents execution of dangerous system functions
3. **Pattern Detection**: Blocks SQL injection patterns, multiple statements
4. **Input Sanitization**: Validates and sanitizes identifiers
5. **Query Limits**: Automatic row limiting (max 10,000), query timeout (30s default)

## Configuration

### Environment Variables

```bash
# Logging
LOG_LEVEL=Information  # Trace, Debug, Information, Warning, Error
LOG_FILE=/var/log/adeotek-sql-mcp.log  # Optional file logging

# .NET Configuration
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
```

## Development

### Project Structure

```
adeotek-sql-net-mcp/
├── src/
│   └── AdeotekSqlMcp/
│       ├── Models/              # Data models
│       ├── Services/            # Business logic
│       ├── Database/            # Database implementations
│       ├── Security/            # Query validation
│       ├── Utilities/           # Logging, errors
│       └── Program.cs           # MCP server
├── tests/
│   └── AdeotekSqlMcp.Tests/    # Unit tests
├── Dockerfile                   # Docker build
├── docker-compose.yml           # Docker orchestration
└── README.md                    # This file
```

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Specific test
dotnet test --filter QueryValidatorTests

# Watch mode
dotnet watch test
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish
```

## Usage Examples

### With Claude Desktop

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "adeotek-sql-net": {
      "command": "dotnet",
      "args": ["/path/to/AdeotekSqlMcp.dll"],
      "env": {
        "LOG_LEVEL": "Information"
      }
    }
  }
}
```

### With MCP Client

```bash
# Start server
dotnet run --project src/AdeotekSqlMcp

# Send initialize request (via stdio)
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run

# List tools
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | dotnet run

# Execute tool
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"sql_list_databases","arguments":{"connectionString":"..."}}}' | dotnet run
```

## Troubleshooting

### Connection Issues

**Problem**: "Connection refused"
**Solution**: Verify database is running, check host/port, verify credentials

**Problem**: "SSL connection required"
**Solution**: Add `ssl=true` or `encrypt=true` to connection string

### Query Validation Issues

**Problem**: "Query validation failed: Blocked keyword detected"
**Solution**: Ensure query starts with SELECT/WITH/EXPLAIN, remove modification keywords

**Problem**: "Multiple statements are not allowed"
**Solution**: Remove semicolons between statements, execute queries one at a time

### Docker Issues

**Problem**: "Docker build fails"
**Solution**: Run `docker-compose build --no-cache adeotek-sql-net-mcp`

**Problem**: "Cannot connect to database"
**Solution**: Ensure databases are healthy: `docker-compose ps`, wait for healthchecks

## Performance

- **Connection Pooling**: Automatic connection reuse per database
- **Query Timeouts**: Default 30 seconds, configurable
- **Row Limits**: Automatic enforcement (max 10,000 rows)
- **Async/Await**: All I/O operations are asynchronous
- **Efficient Queries**: Database-specific optimized system catalog queries

## Comparison with TypeScript Version

| Feature | adeotek-sql-net-mcp (.NET 10) | adeotek-sql-mcp (TypeScript) |
|---------|-------------------------------|------------------------------|
| Language | C# .NET 10 | TypeScript/Node.js |
| Databases | PostgreSQL + SQL Server | PostgreSQL + SQL Server |
| MCP Protocol | 2025-11-25 (stdio) | 2025-11-25 (stdio) |
| Tools | 5 tools | 5 tools |
| Prompts | 3 prompts | 3 prompts |
| Performance | High (compiled) | Good (interpreted) |
| Memory | Lower | Higher |
| Startup Time | Fast | Faster |

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Ensure all tests pass: `dotnet test`
6. Submit a pull request

## License

MIT License - see LICENSE file

## Related Projects

- **adeotek-sql-mcp** (TypeScript): Original TypeScript implementation
- **postgres-mcp** (.NET): PostgreSQL-only MCP server
- **postgres-nl-mcp** (.NET): AI-powered PostgreSQL MCP server

## Support

- **Issues**: https://github.com/adeotek/adeotek-ai-tools/issues
- **Documentation**: See CLAUDE.md for detailed technical context
- **Repository**: https://github.com/adeotek/adeotek-ai-tools

## Changelog

### Version 1.0.0 (2024-12-09)
- Initial release
- Support for PostgreSQL and SQL Server
- 5 MCP tools implemented
- 3 MCP prompts implemented
- Comprehensive security validation
- >80% test coverage
- Docker support
