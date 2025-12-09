# Adeotek SQL .NET MCP Server - Context for Claude

This document provides comprehensive technical context about the Adeotek SQL .NET MCP Server for Claude (both CLI and Web) interactions.

## Project Overview

**Purpose**: A production-ready Model Context Protocol (MCP) server providing read-only access to SQL databases (Microsoft SQL Server and PostgreSQL) with comprehensive security safeguards. This is a .NET 10 port of the TypeScript `adeotek-sql-mcp` project.

**Version**: 1.0.0

**MCP Protocol**: 2025-11-25 (stdio transport)

**Technology Stack**: .NET 10, C# 13, Npgsql 9.0+, Microsoft.Data.SqlClient 6.0+, Serilog, xUnit, NSubstitute

**Location in Repository**: `/mcp-servers/adeotek-sql-net-mcp`

**Key Capabilities**:
- Full MCP Protocol 2025-11-25 implementation with stdio transport
- Five MCP tools for database operations
- Three MCP prompts for schema analysis and query assistance
- Multi-database support (PostgreSQL and SQL Server)
- Comprehensive read-only security with multiple validation layers
- .NET 10 with C# 13 features and full type safety
- Extensive test coverage (>80%)

## Architecture

### Project Structure

```
adeotek-sql-net-mcp/
├── src/
│   └── AdeotekSqlMcp/
│       ├── Models/
│       │   ├── McpModels.cs              # MCP protocol models
│       │   └── DatabaseModels.cs         # Database schema models
│       ├── Services/
│       │   ├── McpToolsService.cs        # All 5 MCP tools
│       │   └── McpPromptsService.cs      # All 3 MCP prompts
│       ├── Database/
│       │   ├── IDatabase.cs              # Database interface
│       │   ├── PostgresDatabase.cs       # PostgreSQL implementation
│       │   ├── SqlServerDatabase.cs      # SQL Server implementation
│       │   ├── ConnectionStringParser.cs # Connection string parsing
│       │   └── DatabaseFactory.cs        # Factory for database instances
│       ├── Security/
│       │   └── QueryValidator.cs         # Multi-layer query validation
│       ├── Utilities/
│       │   ├── McpException.cs           # Custom exceptions
│       │   └── Logger.cs                 # Serilog configuration
│       ├── Program.cs                    # MCP server with stdio transport
│       └── AdeotekSqlMcp.csproj
├── tests/
│   └── AdeotekSqlMcp.Tests/
│       ├── QueryValidatorTests.cs
│       ├── ConnectionStringParserTests.cs
│       └── AdeotekSqlMcp.Tests.csproj
├── Dockerfile
├── docker-compose.yml
├── .editorconfig
├── .gitignore
├── README.md
└── CLAUDE.md                             # This file
```

### Design Patterns

1. **Factory Pattern**: `DatabaseFactory` creates appropriate database instances (PostgreSQL or SQL Server)
2. **Strategy Pattern**: Different database implementations with common `IDatabase` interface
3. **Validation Pattern**: Multi-layer query validation for security (`QueryValidator`)
4. **Dependency Injection**: Services created and injected in Program.cs
5. **MCP Protocol Pattern**: Tools and prompts registered with MCP server
6. **JSON-RPC 2.0 Pattern**: Standard request/response handling

### Key Components

**Program.cs**:
- MCP server implementation using stdio transport
- JSON-RPC 2.0 request/response handling
- Tool and prompt routing
- Request parsing and error handling

**Services**:
- **McpToolsService**: Implements all 5 MCP tools with argument parsing and execution
- **McpPromptsService**: Implements all 3 MCP prompts with template substitution

**Database Implementations**:
- **PostgresDatabase**: PostgreSQL operations using Npgsql
- **SqlServerDatabase**: SQL Server operations using Microsoft.Data.SqlClient
- **DatabaseFactory**: Creates appropriate database instance based on connection string
- **ConnectionStringParser**: Parses connection string format

**Security**:
- **QueryValidator**: Multi-layer SQL validation with blocked keywords, dangerous functions, pattern detection

**Models**:
- **McpModels**: MCP protocol types (Tool, Prompt, JsonRpcRequest, JsonRpcResponse)
- **DatabaseModels**: Database schema types (DatabaseInfo, TableInfo, TableSchema, QueryResult)

**Utilities**:
- **McpException**: Custom exception hierarchy with error codes
- **Logger**: Serilog-based logging with sensitive data sanitization

## MCP Protocol Implementation

This server implements MCP Protocol 2025-11-25 with stdio transport.

### Supported MCP Methods

**Lifecycle Methods**:
- `initialize`: Initialize MCP connection with capabilities
- `initialized`: Confirmation of initialization

**Tool Methods**:
- `tools/list`: List all available tools
- `tools/call`: Execute a tool with arguments

**Prompt Methods**:
- `prompts/list`: List all available prompts
- `prompts/get`: Get a prompt with argument substitution

### Request/Response Format

All requests follow JSON-RPC 2.0 format:

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "sql_list_databases",
    "arguments": {
      "connectionString": "type=postgres;host=localhost;..."
    }
  }
}
```

**Success Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "success": true,
    "data": { ... }
  }
}
```

**Error Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "Query validation failed",
    "data": "Blocked keyword detected: INSERT"
  }
}
```

## MCP Tools

### Tool 1: sql_list_databases

**Purpose**: List all databases on the server

**Implementation**: `McpToolsService.ListDatabasesAsync()`

**Database-Specific Queries**:
- PostgreSQL: Queries `pg_database` system catalog
- SQL Server: Queries `sys.databases` catalog view

**Security**: Read-only system catalog access

### Tool 2: sql_list_tables

**Purpose**: List all tables in a database with metadata

**Implementation**: `McpToolsService.ListTablesAsync()`

**Database-Specific Queries**:
- PostgreSQL: Queries `pg_tables` and `pg_views`
- SQL Server: Queries `sys.tables` and `sys.views` with row counts

**Security**: Identifier sanitization, schema filtering

### Tool 3: sql_describe_table

**Purpose**: Get detailed schema information for a table

**Implementation**: `McpToolsService.DescribeTableAsync()`

**Returns**:
- Columns (name, type, nullable, default, PK/FK status)
- Indexes (name, columns, unique, primary)
- Foreign keys (name, columns, referenced table/columns)
- Constraints (name, type, definition)

**Database-Specific**: Complex queries to `information_schema` and system catalogs

### Tool 4: sql_query

**Purpose**: Execute read-only SELECT query with validation

**Implementation**: `McpToolsService.ExecuteQueryAsync()`

**Security Layers**:
1. `QueryValidator.ValidateOrThrow()`: Checks for blocked keywords and patterns
2. `QueryValidator.EnforceLimit()`: Adds/enforces LIMIT clause (max 10,000 rows)
3. Database query timeout (30 seconds default)
4. Result set size limits

**Validation**:
- Must start with SELECT, WITH, or EXPLAIN
- Blocks INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, etc.
- Blocks dangerous functions (pg_read_file, xp_cmdshell, etc.)
- Prevents multiple statements (semicolon detection)
- Detects SQL injection patterns

### Tool 5: sql_get_query_plan

**Purpose**: Get execution plan without executing query

**Implementation**: `McpToolsService.GetQueryPlanAsync()`

**Database-Specific**:
- PostgreSQL: Uses `EXPLAIN (FORMAT JSON, ANALYZE false)`
- SQL Server: Uses `SET SHOWPLAN_XML ON/OFF`

**Security**: Same validation as sql_query

## MCP Prompts

### Prompt 1: analyze-schema

**Purpose**: Analyze database schema and provide insights

**Arguments**:
- `database` (required): Database to analyze
- `focus` (optional): Specific focus area

**Template**: Guides AI to analyze tables, relationships, integrity, indexes, naming, normalization

### Prompt 2: query-assistant

**Purpose**: Help construct SQL queries from natural language

**Arguments**:
- `database` (required): Target database
- `requirement` (required): Natural language description

**Template**: Guides AI to understand schema, construct query, explain, optimize

### Prompt 3: performance-review

**Purpose**: Review query performance and suggest optimizations

**Arguments**:
- `database` (required): Database name
- `query` (required): SQL query to analyze

**Template**: Guides AI to analyze execution plan, identify bottlenecks, suggest indexes

## Read-Only Security

### Validation Layers

**Layer 1: Keyword Blocking**

Blocks these operations:
- Data modification: INSERT, UPDATE, DELETE, TRUNCATE, MERGE, UPSERT, REPLACE, COPY
- Schema modification: CREATE, ALTER, DROP, RENAME, COMMENT
- Permissions: GRANT, REVOKE
- Transaction control: BEGIN, COMMIT, ROLLBACK, SAVEPOINT
- Locking: LOCK, UNLOCK
- Maintenance: VACUUM, ANALYZE, REINDEX, CLUSTER, CHECKPOINT
- Configuration: SET, RESET
- Messaging: LISTEN, NOTIFY, UNLISTEN
- Procedural: DO, CALL, EXECUTE, DECLARE

**Layer 2: Function Blocking**

Blocks dangerous functions:
- PostgreSQL: `pg_read_file`, `pg_read_binary_file`, `pg_execute`, `pg_terminate_backend`, `pg_sleep`
- SQL Server: `xp_cmdshell`, `sp_executesql`, `OPENROWSET`, `OPENDATASOURCE`

**Layer 3: Pattern Detection**

Blocks dangerous patterns:
- Multiple statements (semicolons)
- SQL injection attempts
- Procedural code blocks ($$)
- INTO OUTFILE
- LOAD_FILE

**Layer 4: Input Sanitization**

Sanitizes identifiers:
- Removes non-alphanumeric characters (except underscore and dot)
- Prevents SQL injection in database/table/column names

**Layer 5: Query Limits**

Enforces limits:
- Maximum rows: 10,000 (configurable down to 1,000)
- Query timeout: 30 seconds (configurable)
- Query length: 50,000 characters max

### Validation Implementation

```csharp
public ValidationResult Validate(string query)
{
    // 1. Check for empty query
    // 2. Check query length
    // 3. Normalize and check starting keyword
    // 4. Check for blocked keywords (regex-based)
    // 5. Check for blocked functions (regex-based)
    // 6. Check for dangerous patterns
    // 7. Check for multiple statements
    // 8. Generate warnings (missing LIMIT, SELECT *)

    return new ValidationResult { IsValid, Errors, Warnings };
}
```

## Configuration

### Connection String Format

**PostgreSQL**:
```
type=postgres;host=localhost;port=5432;user=myuser;password=mypass;database=mydb;ssl=true
```

**SQL Server**:
```
type=mssql;host=localhost;port=1433;user=sa;password=StrongPass123;database=master;ssl=true
```

**Supported Keys**:
- `type`: "postgres" or "mssql" (required)
- `host` / `server` / `data source`: Database server host (required)
- `port`: Port number (default: 5432 for PostgreSQL, 1433 for SQL Server)
- `user` / `username` / `user id` / `uid`: Database user (required)
- `password` / `pwd`: Database password (required)
- `database` / `initial catalog`: Database name (optional)
- `connectionTimeout` / `connect timeout`: Connection timeout in seconds
- `commandTimeout` / `request timeout`: Query timeout in seconds
- `ssl` / `encrypt`: Enable SSL/TLS (true/false)

### Environment Variables

- `LOG_LEVEL`: Logging level (Trace, Debug, Information, Warning, Error) - default: Information
- `LOG_FILE`: Log file path (optional) - enables file logging
- `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`: .NET globalization setting

## Development Workflow

### Local Development Setup

```bash
# Prerequisites: .NET 10 SDK

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the server
dotnet run --project src/AdeotekSqlMcp

# Watch mode (auto-rebuild)
dotnet watch run --project src/AdeotekSqlMcp
```

### Running Tests

```bash
# All tests
dotnet test

# With verbose output
dotnet test --verbosity normal

# Specific test class
dotnet test --filter QueryValidatorTests

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Watch mode
dotnet watch test
```

### Code Quality

```bash
# Format code
dotnet format

# Build with warnings as errors
dotnet build /warnaserror

# Clean
dotnet clean

# Publish
dotnet publish -c Release -o ./publish
```

## Testing

### Test Categories

**Security Tests** (`QueryValidatorTests.cs`):
- Valid SELECT queries
- Blocked modification operations
- Multiple statement detection
- Dangerous function detection
- Identifier sanitization
- Query limit enforcement

**Configuration Tests** (`ConnectionStringParserTests.cs`):
- Connection string parsing
- PostgreSQL configuration
- SQL Server configuration
- Alternative key formats
- Error handling

### Writing Tests

Follow xUnit and FluentAssertions patterns:

```csharp
public class MyTests
{
    [Fact]
    public void Method_Scenario_ExpectedResult()
    {
        // Arrange
        var input = ...;

        // Act
        var result = method(input);

        // Assert
        result.Should().Be(...);
    }

    [Theory]
    [InlineData(...)]
    public void Method_MultipleScenarios_ExpectedResults(string input)
    {
        // ...
    }
}
```

## Comparison with TypeScript Version

| Feature | adeotek-sql-net-mcp (.NET 10) | adeotek-sql-mcp (TypeScript) |
|---------|-------------------------------|------------------------------|
| Language | C# .NET 10 | TypeScript 5.7/Node.js 18+ |
| MCP Protocol | 2025-11-25 (stdio) | 2025-11-25 (stdio) |
| Databases | PostgreSQL + SQL Server | PostgreSQL + SQL Server |
| Tools | 5 tools | 5 tools |
| Prompts | 3 prompts | 3 prompts |
| Performance | High (compiled, AOT) | Good (JIT) |
| Memory Usage | Lower (native) | Higher (V8) |
| Startup Time | Fast | Faster |
| Type Safety | Compile-time | Runtime |
| Package Size | Larger binary | Smaller with dependencies |

**When to use adeotek-sql-net-mcp**:
- Prefer .NET ecosystem
- Need better performance
- Lower memory footprint required
- Existing .NET infrastructure

**When to use adeotek-sql-mcp**:
- Prefer TypeScript/Node.js ecosystem
- Faster cold starts required
- Smaller deployment size needed
- More familiar with JavaScript

## Error Handling

### Custom Exception Classes

```csharp
// Base exception
public class McpException : Exception

// Specific exceptions
public sealed class DatabaseConnectionException : McpException
public sealed class QueryValidationException : McpException
public sealed class QueryExecutionException : McpException
public sealed class ConfigurationException : McpException
public sealed class TimeoutException : McpException
public sealed class ToolNotFoundException : McpException
public sealed class PromptNotFoundException : McpException
```

### Error Handling Pattern

```csharp
try
{
    // Execute operation
}
catch (McpException ex)
{
    return new McpToolCallResponse
    {
        Success = false,
        Error = ex.Message,
        Metadata = new Dictionary<string, object>
        {
            ["errorCode"] = ex.ErrorCode
        }
    };
}
```

## Logging

Uses Serilog with:
- Structured logging
- Timestamp in all logs
- Log levels: Verbose, Debug, Information, Warning, Error, Fatal
- Sensitive data sanitization (passwords, API keys)
- Console output (colored)
- Optional file output

### Logging Levels

- **Verbose/Trace**: Very detailed debugging
- **Debug**: Detailed operation info
- **Information**: Operation starts/completions
- **Warning**: Validation warnings, potential issues
- **Error**: Failures and exceptions
- **Fatal**: Critical failures

## Performance Considerations

- **Connection Pooling**: Reuses connections per database
- **Query Timeouts**: Prevents long-running queries (30s default)
- **Row Limits**: Automatic LIMIT enforcement (max 10,000)
- **Async/Await**: All I/O operations are asynchronous
- **Efficient Queries**: Database-specific optimized queries
- **Compiled Code**: AOT compilation for better performance

## Troubleshooting

### Common Issues

**Issue**: Connection string parsing fails
**Solution**: Check format matches `key=value;key=value` pattern, ensure required fields (type, host, user, password)

**Issue**: Query validation fails
**Solution**: Ensure query starts with SELECT/WITH/EXPLAIN, remove modification keywords, check for dangerous patterns

**Issue**: Query timeout
**Solution**: Optimize query, add indexes, use WHERE clauses, add LIMIT

### Debug Mode

Set `LOG_LEVEL=Debug` for detailed logging:

```bash
LOG_LEVEL=Debug dotnet run --project src/AdeotekSqlMcp
```

## Working with Claude

### When Adding Features

1. **Read existing code**: Use Read tool to understand implementation
2. **Follow .NET conventions**: Use modern C# 13 features (records, init-only, required)
3. **Update tests**: Add xUnit tests for new functionality
4. **Update documentation**: Update README.md and this CLAUDE.md
5. **Test thoroughly**: Run `dotnet test` and `dotnet build`

### When Debugging

1. **Check logs**: Look at Serilog output
2. **Run tests**: `dotnet test` to identify failures
3. **Review error classes**: Check `Utilities/McpException.cs`
4. **Enable debug logging**: Set `LOG_LEVEL=Debug`

### When Refactoring

1. **Maintain MCP compliance**: Don't break tool/prompt APIs
2. **Update tests**: Ensure all tests pass
3. **Follow patterns**: Maintain consistency
4. **Document changes**: Update comments and docs
5. **Preserve security**: Don't compromise read-only safeguards

## Code Quality Standards

### .NET Standards

- Use .NET 10 and C# 13 features (records, init-only, required)
- Follow .NET naming conventions
- Implement dependency injection
- Use async/await throughout
- Add XML documentation comments
- Implement IDisposable/IAsyncDisposable
- Use nullable reference types

### Code Organization

- One class per file
- Use namespaces matching folder structure
- Separate concerns
- Keep files under 500 lines
- Use barrel exports where appropriate

### Security-First Development

- **Always validate queries**: Every query through QueryValidator
- **Multiple validation layers**: Don't rely on single check
- **Test security**: Write tests for blocked operations
- **Document security**: Explain why operations are blocked
- **Never trust input**: Sanitize all user input

## Future Enhancements

- [ ] MySQL support
- [ ] SQLite support
- [ ] Query result caching
- [ ] Streaming for large result sets
- [ ] Query history and auditing
- [ ] Advanced schema analysis
- [ ] Cost estimation before query execution
- [ ] Real-time query monitoring
- [ ] Multi-database query aggregation

## Related Documentation

- **Main Repository Context**: `/CLAUDE.md` - Repository-wide guidelines
- **User Documentation**: `README.md` - User-facing documentation
- **Related Projects**:
  - `/mcp-servers/adeotek-sql-mcp/CLAUDE.md` - TypeScript version
  - `/mcp-servers/postgres-mcp/CLAUDE.md` - .NET PostgreSQL MCP server
  - `/mcp-servers/postgres-nl-mcp/CLAUDE.md` - .NET PostgreSQL with AI

## Questions for Claude

When working on adeotek-sql-net-mcp, you can ask:

**Architecture and Implementation**:
- "How does query validation work?"
- "What operations are blocked and why?"
- "How do I add support for a new database?"
- "How does the database factory work?"

**Database-Specific**:
- "How does the PostgreSQL implementation differ from SQL Server?"
- "What system catalogs are queried?"
- "How are execution plans retrieved?"

**Security**:
- "How is SQL injection prevented?"
- "What are the validation layers?"
- "How do I add a new blocked keyword?"

**Testing and Development**:
- "How do I write tests for a new tool?"
- "How do I test with a real database?"
- "How do I debug connection issues?"
- "How do I add a new MCP tool?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for the Adeotek SQL .NET MCP Server project.
