# PostgreSQL MCP Server - Context for Claude

This document provides comprehensive context about the PostgreSQL MCP Server for Claude (both CLI and Web) interactions.

## Project Overview

**Purpose**: A **read-only** Model Context Protocol (MCP) server for PostgreSQL database operations. This is a simple, lightweight MCP server that provides secure, read-only access to PostgreSQL databases for AI agents without requiring AI/LLM dependencies.

**Technology Stack**: .NET 9, ASP.NET Core, Npgsql, Serilog, Scalar, AspNetCoreRateLimit

**Location in Repository**: `/mcp-servers/postgres-mcp`

**Key Difference from postgres-nl-mcp**: This server does NOT use AI/LLM for query generation. It provides direct SQL query execution with strict read-only validation. For AI-powered natural language queries, see `/mcp-servers/postgres-nl-mcp`.

## Architecture

### Project Structure

```
postgres-mcp/
├── src/
│   ├── PostgresMcp/              # Main application
│   │   ├── Controllers/          # MCP API controllers
│   │   │   └── McpController.cs  # MCP tools implementation
│   │   ├── Models/               # Data models
│   │   │   ├── McpModels.cs      # MCP protocol models
│   │   │   ├── DatabaseModels.cs # Database schema models
│   │   │   └── ConfigurationModels.cs # Configuration options
│   │   ├── Services/             # Business logic
│   │   │   ├── DatabaseService.cs       # Database operations
│   │   │   └── QueryValidationService.cs # SQL validation
│   │   ├── Program.cs            # Application entry point
│   │   ├── appsettings.json      # Default configuration
│   │   └── appsettings.Development.json # Development overrides
│   └── PostgresMcp.Tests/        # Unit tests
├── tests/
│   └── PostgresMcp.Tests/        # xUnit tests
├── docker-init/                  # Database initialization scripts
├── Dockerfile                    # Docker build
├── docker-compose.yml            # Docker orchestration
├── Makefile                      # Build automation
├── README.md                     # User-facing documentation
└── CLAUDE.md                     # This file - Claude context
```

### Design Patterns

1. **Dependency Injection**: All services registered in ASP.NET Core DI container
2. **Options Pattern**: Configuration via strongly-typed `IOptions<T>`
3. **Repository Pattern**: Database access abstraction (DatabaseService)
4. **Validation Pattern**: Multi-layer query validation for security (QueryValidationService)
5. **MCP Protocol**: JSON-RPC 2.0 compliant tool discovery and execution

### Key Components

**Controllers** (`Controllers/McpController.cs`):
- RESTful API endpoints for MCP operations
- JSON-RPC 2.0 endpoint for standard MCP clients
- Input validation and error handling
- Rate limiting middleware integration

**Services**:
- **DatabaseService**: Executes database queries, retrieves schema information
- **QueryValidationService**: Validates SQL queries for read-only compliance

**Models**:
- **McpModels**: MCP protocol request/response structures
- **DatabaseModels**: Schema representation (tables, columns, foreign keys, indexes)
- **ConfigurationModels**: Typed configuration for PostgreSQL and security settings

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

## Configuration

### Configuration Methods

The application supports multiple configuration methods (in priority order):
1. **Environment variables** (highest priority)
2. **User secrets** (development only)
3. **appsettings.{Environment}.json**
4. **appsettings.json** (default values, lowest priority)

### Environment Variables

**PostgreSQL Configuration**:
```bash
Postgres__DefaultConnectionString="Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass;SSL Mode=Require"
Postgres__MaxRetries=3
Postgres__ConnectionTimeoutSeconds=30
Postgres__CommandTimeoutSeconds=60
Postgres__UseSsl=true
```

**Security Settings**:
```bash
Security__EnableRateLimiting=true                # Enable rate limiting
Security__RequestsPerMinute=60                   # Max requests per minute per IP
Security__MaxRowsPerQuery=10000                  # Max rows returned per query
Security__MaxQueryExecutionSeconds=30            # Query timeout
```

**Logging**:
```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__PostgresMcp=Debug
Logging__LogQueries=false                        # Log executed SQL queries
Logging__LogResults=false                        # Log query results (be careful with sensitive data)
```

**Application Settings**:
```bash
ASPNETCORE_ENVIRONMENT=Development               # Development, Staging, Production
ASPNETCORE_URLS=http://+:5000;https://+:5001    # Listening URLs
```

### User Secrets (Development)

For local development, use .NET user secrets (never committed to version control):

```bash
cd src/PostgresMcp
dotnet user-secrets init
dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;Database=testdb;Username=postgres;Password=yourpass"
```

## Development Workflow

### Local Development Setup

1. **Install prerequisites**:
   ```bash
   # Install .NET 9 SDK
   # Install PostgreSQL 16+ (or use Docker)
   ```

2. **Clone and navigate**:
   ```bash
   cd mcp-servers/postgres-mcp
   ```

3. **Set up configuration**:
   ```bash
   cd src/PostgresMcp
   dotnet user-secrets init
   dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;Database=testdb;Username=postgres;Password=yourpass"
   ```

4. **Restore and build**:
   ```bash
   dotnet restore
   dotnet build
   ```

5. **Run the application**:
   ```bash
   dotnet run
   # Or for hot reload:
   dotnet watch run
   ```

6. **Access the application**:
   - API: http://localhost:5000
   - API Documentation: http://localhost:5000/scalar/v1
   - Health check: http://localhost:5000/health

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

### `GET /mcp/tools`

Lists all available MCP tools with their schemas.

**Response**:
```json
{
  "tools": [
    {
      "name": "scan_database_structure",
      "description": "Scan and analyze PostgreSQL database structure...",
      "inputSchema": {
        "type": "object",
        "properties": {
          "connectionString": { "type": "string" },
          "schemaFilter": { "type": "string" }
        },
        "required": ["connectionString"]
      }
    },
    {
      "name": "query_database",
      "description": "Execute a read-only SELECT query...",
      "inputSchema": {
        "type": "object",
        "properties": {
          "connectionString": { "type": "string" },
          "query": { "type": "string" }
        },
        "required": ["connectionString", "query"]
      }
    }
  ]
}
```

### `POST /mcp/tools/call`

Executes an MCP tool.

**Request**:
```json
{
  "name": "scan_database_structure",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass",
    "schemaFilter": "public"
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "tables": [
      {
        "schema": "public",
        "name": "customers",
        "columns": [...],
        "primaryKey": [...],
        "foreignKeys": [...],
        "indexes": [...],
        "estimatedRowCount": 1523
      }
    ]
  },
  "metadata": {
    "executedAt": "2024-12-02T10:30:00Z",
    "tableCount": 15
  },
  "errorMessage": null
}
```

### `POST /mcp/jsonrpc`

JSON-RPC 2.0 endpoint for standard MCP clients.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "query_database",
    "arguments": {
      "connectionString": "...",
      "query": "SELECT * FROM customers LIMIT 10"
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
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

### `GET /health`

Health check endpoint.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-12-02T10:30:00Z",
  "version": "1.0.0"
}
```

### `GET /scalar/v1`

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

Follow xUnit conventions:

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

1. **Understand the read-only constraint**: This server must NEVER allow write operations
2. **Read the code first**: Use the Read tool to understand existing implementation
3. **Follow .NET conventions**: Use standard .NET/C# patterns and idioms
4. **Update tests**: Add xUnit tests for new functionality
5. **Update validation**: Ensure new features maintain read-only security
6. **Update documentation**: Update both README.md and this CLAUDE.md
7. **Test thoroughly**: Run tests, build, and test locally with Docker

### When Debugging

1. **Check logs**: Look at Serilog output in console or log files
2. **Verify configuration**: Check user secrets, environment variables, appsettings.json
3. **Test endpoints**: Use curl, Postman, or the Scalar UI
4. **Review query validation**: Check QueryValidationService for blocked patterns
5. **Review code**: Check for common .NET issues (null references, async/await, disposal)
6. **Enable debug mode**: Set `ASPNETCORE_ENVIRONMENT=Development` and logging to Debug

### When Refactoring

1. **Maintain read-only security**: Never compromise on query validation
2. **Maintain compatibility**: Don't break the MCP protocol API
3. **Update tests**: Ensure all tests pass after changes
4. **Follow patterns**: Maintain consistency with ASP.NET Core best practices
5. **Document changes**: Update XML documentation comments

## Code Quality Standards

### .NET Best Practices

- Use .NET 9 and C# 13 features
- Follow .NET naming conventions (PascalCase for public, camelCase for private)
- Implement dependency injection via built-in DI container
- Use async/await throughout (all I/O operations should be async)
- Add XML documentation comments for public APIs
- Implement IDisposable/IAsyncDisposable when managing resources
- Use nullable reference types and handle nullability properly

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

## Future Enhancements

- [ ] Schema caching for faster structure scans
- [ ] Query result caching with TTL
- [ ] Support for parameterized queries
- [ ] Query history and auditing
- [ ] Connection string encryption at rest
- [ ] Multi-database support (multiple connections)
- [ ] Query cost estimation before execution
- [ ] Real-time query monitoring
- [ ] Saved queries and templates
- [ ] Export formats (CSV, JSON, Excel)
- [ ] Authentication and authorization
- [ ] WebSocket support for streaming results

## Related Documentation

- **Main Repository Context**: `/CLAUDE.md` - Repository-wide guidelines and patterns
- **User Documentation**: `README.md` - User-facing documentation
- **Configuration Files**: `appsettings.json`, `appsettings.Development.json`
- **Docker Setup**: `docker-compose.yml`, `Dockerfile`
- **Related Project**: `/mcp-servers/postgres-nl-mcp/CLAUDE.md` - AI-powered variant

## Questions for Claude

When working on the PostgreSQL MCP Server, you can ask:

- "How does query validation work?"
- "What operations are blocked and why?"
- "How do I add a new validation rule?"
- "How does the scan_database_structure tool work?"
- "How do I test with a local database?"
- "How does rate limiting work?"
- "What's the difference between postgres-mcp and postgres-nl-mcp?"
- "Where should I add a new configuration option?"
- "How does the MCP protocol implementation work?"
- "How do I ensure a new feature maintains read-only security?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for the PostgreSQL MCP Server project.
