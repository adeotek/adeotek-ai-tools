# PostgreSQL Natural Language MCP Server - Context for Claude

This document provides comprehensive context about the PostgreSQL Natural Language MCP Server for Claude (both CLI and Web) interactions.

## Project Overview

**Purpose**: A production-ready Model Context Protocol (MCP) server for PostgreSQL database operations with AI-powered query generation and natural language understanding.

**Technology Stack**: .NET 9, ASP.NET Core, Npgsql, Semantic Kernel, Scalar, Serilog, xUnit

**Location in Repository**: `/mcp-servers/postgres-nl-mcp`

**Status**: ✅ Production Ready

## Architecture

### Project Structure

```
postgres-nl-mcp/
├── src/
│   └── PostgresNaturalLanguageMcp/
│       ├── Controllers/          # API endpoints
│       │   └── McpController.cs  # MCP tool implementation
│       ├── Services/             # Business logic
│       │   ├── DatabaseSchemaService.cs      # Schema analysis
│       │   ├── QueryService.cs               # Query execution
│       │   └── SqlGenerationService.cs       # AI-powered SQL generation
│       ├── Models/               # Data models
│       │   ├── McpModels.cs                  # MCP protocol models
│       │   ├── DatabaseModels.cs             # Database schema models
│       │   └── ConfigurationModels.cs        # Configuration options
│       ├── appsettings.json      # Default configuration
│       ├── appsettings.Development.json      # Development overrides
│       └── Program.cs            # Application startup
├── tests/
│   └── PostgresNaturalLanguageMcp.Tests/     # Unit tests
│       ├── Services/             # Service tests
│       └── Controllers/          # Controller tests
├── docker/
│   └── init.sql                  # Sample database initialization
├── Dockerfile                    # Docker build file
├── docker-compose.yml            # Docker Compose config
├── README.md                     # User-facing documentation
└── CLAUDE.md                     # This file - Claude context
```

### Design Patterns

1. **Dependency Injection**: All services registered in ASP.NET Core DI container
2. **Options Pattern**: Configuration via strongly-typed `IOptions<T>`
3. **Repository Pattern**: Database access abstraction (DatabaseSchemaService, QueryService)
4. **Strategy Pattern**: Multiple AI providers via Semantic Kernel (OpenAI, Azure OpenAI, Anthropic, Gemini, Ollama, LM Studio)
5. **MCP Protocol**: JSON-RPC 2.0 compliant tool discovery and execution

### Key Components

**Controllers** (`Controllers/McpController.cs`):
- RESTful API endpoints for MCP operations
- JSON-RPC 2.0 endpoint for standard MCP clients
- Input validation and error handling
- Rate limiting middleware integration

**Services**:
- **DatabaseSchemaService**: Analyzes PostgreSQL database schema (tables, columns, relationships, constraints)
- **QueryService**: Executes queries with security validation and result formatting
- **SqlGenerationService**: Generates SQL from natural language using AI

**Models**:
- **McpModels**: MCP protocol request/response structures
- **DatabaseModels**: Schema representation (tables, columns, foreign keys, indexes)
- **ConfigurationModels**: Typed configuration for PostgreSQL, AI, and security settings

## MCP Tools

### Tool 1: scan_database_structure

Comprehensive database schema analysis with AI-powered insights.

**Capabilities**:
- List all tables, views, and materialized views
- Display detailed column information (data types, constraints, defaults)
- Show primary keys, foreign keys, and indexes
- Identify table relationships and cardinality
- Answer natural language questions about schema structure
- Provide statistics (row counts, table sizes)

**Input Schema**:
```json
{
  "connectionString": "Host=...;Database=...;Username=...;Password=...",
  "schemaFilter": "public",  // optional, filter by schema name
  "question": "What tables have foreign keys to the customers table?"  // optional
}
```

**Example Use Cases**:
- "What tables have foreign keys to the customers table?"
- "Show me all tables in the public schema"
- "What is the structure of the orders table?"
- "List all foreign key relationships"

### Tool 2: query_database_data

Intelligent data querying with automatic relationship detection.

**Capabilities**:
- Convert natural language to SQL queries
- Automatically follow foreign key relationships
- Return structured JSON results with metadata
- Include execution metadata (row count, execution time)
- Handle complex multi-table queries with JOINs
- Respect security limits (max rows, query timeout)

**Input Schema**:
```json
{
  "connectionString": "Host=...;Database=...;Username=...;Password=...",
  "query": "Show me all customers who made orders in the last 30 days"
}
```

**Example Use Cases**:
- "Show me all customers who made orders in the last 30 days"
- "Get the top 10 products by revenue"
- "List all orders with customer names and addresses"
- "Find customers with more than 5 orders"

### Tool 3: advanced_sql_query

AI-powered SQL generation with validation and optimization.

**Capabilities**:
- Generate SQL from detailed natural language descriptions
- Validate query safety (SQL injection prevention, data modification blocking)
- Optimize queries for performance
- Provide query explanations and confidence scores
- Support complex aggregations, joins, subqueries, window functions
- Return both the generated SQL and the query results

**Input Schema**:
```json
{
  "connectionString": "Host=...;Database=...;Username=...;Password=...",
  "naturalLanguageQuery": "Calculate average order value by customer segment for Q4 2024"
}
```

**Example Use Cases**:
- "Calculate average order value by product category for customers with more than 3 orders"
- "Find customers who haven't ordered in 90 days but had more than 5 orders in their lifetime"
- "Analyze monthly sales trends with year-over-year comparison"
- "Show top 10 customers by total revenue with their contact information"

## LLM Provider Support

The server supports multiple LLM providers via Semantic Kernel:

### Cloud Providers

**OpenAI** (default):
```bash
Ai__Provider=openai
Ai__ApiKey=sk-...
Ai__Model=gpt-4
```

**Anthropic Claude**:
```bash
Ai__Provider=anthropic
Ai__ApiKey=sk-ant-...
Ai__Model=claude-3-5-sonnet-20241022
```

**Google Gemini**:
```bash
Ai__Provider=gemini
Ai__ApiKey=AIza...
Ai__Model=gemini-1.5-pro
```

**Azure OpenAI**:
```bash
Ai__Provider=azureopenai
Ai__ApiKey=your-azure-key
Ai__AzureEndpoint=https://your-resource.openai.azure.com
Ai__AzureDeploymentName=gpt-4
Ai__Model=gpt-4
```

### Local Providers

**Ollama**:
```bash
Ai__Provider=ollama
Ai__Model=llama3
Ai__BaseUrl=http://localhost:11434
# Requires: ollama serve && ollama pull llama3
```

**LM Studio**:
```bash
Ai__Provider=lmstudio
Ai__Model=local-model
Ai__BaseUrl=http://localhost:1234
# Requires: LM Studio running with local server enabled
```

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

**AI Configuration**:
```bash
Ai__Provider=openai          # openai, anthropic, gemini, ollama, lmstudio, azureopenai
Ai__ApiKey=sk-...            # API key for cloud providers
Ai__Model=gpt-4              # Model name
Ai__BaseUrl=                 # Only for ollama/lmstudio
Ai__Enabled=true             # Enable/disable AI features
Ai__MaxTokens=2000           # Max tokens for AI responses
Ai__Temperature=0.1          # Temperature (0.0-1.0, lower = more deterministic)

# Azure OpenAI specific:
Ai__AzureEndpoint=https://your-resource.openai.azure.com
Ai__AzureDeploymentName=gpt-4
```

**Security Settings**:
```bash
Security__EnableRateLimiting=true                # Enable rate limiting
Security__RequestsPerMinute=60                   # Max requests per minute per IP
Security__MaxRowsPerQuery=10000                  # Max rows returned per query
Security__MaxQueryExecutionSeconds=30            # Query timeout
Security__AllowDataModification=false            # Allow INSERT/UPDATE/DELETE
Security__AllowSchemaModification=false          # Allow DDL operations
```

**Logging**:
```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__PostgresNaturalLanguageMcp=Debug
Logging__LogQueries=false                        # Log executed SQL queries
Logging__LogResults=false                        # Log query results (be careful with sensitive data)
Logging__LogAiInteractions=false                 # Log AI requests/responses
```

**Application Settings**:
```bash
ASPNETCORE_ENVIRONMENT=Development               # Development, Staging, Production
ASPNETCORE_URLS=http://+:5000;https://+:5001    # Listening URLs
```

### User Secrets (Development)

For local development, use .NET user secrets (never committed to version control):

```bash
cd src/PostgresNaturalLanguageMcp
dotnet user-secrets init
dotnet user-secrets set "Ai:ApiKey" "sk-..."
dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;..."
```

## Development Workflow

### Local Development Setup

1. **Install prerequisites**:
   ```bash
   # Install .NET 9 SDK
   # Install PostgreSQL 16+ (or use Docker)
   # Get an LLM API key (OpenAI, Anthropic, Gemini) or set up local LLM (Ollama/LM Studio)
   ```

2. **Clone and navigate**:
   ```bash
   cd mcp-servers/postgres-nl-mcp
   ```

3. **Set up configuration**:
   ```bash
   cd src/PostgresNaturalLanguageMcp
   dotnet user-secrets init
   dotnet user-secrets set "Ai:Provider" "openai"
   dotnet user-secrets set "Ai:ApiKey" "sk-..."
   dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;Database=testdb;..."
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
cd mcp-servers/postgres-nl-mcp
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

## Docker Deployment

### Using Docker Compose (Recommended)

The docker-compose setup includes:
- PostgreSQL Natural Language MCP Server (port 5000)
- PostgreSQL database with sample data (port 5432)
- pgAdmin for database management (port 8080)

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f postgres-nl-mcp

# View all logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (resets database)
docker-compose down -v

# Rebuild after code changes
docker-compose build --no-cache postgres-nl-mcp
docker-compose up -d
```

**Access**:
- MCP Server: http://localhost:5000
- API Documentation: http://localhost:5000/scalar/v1
- PostgreSQL: localhost:5432 (postgres/postgres123)
- pgAdmin: http://localhost:8080 (admin@admin.com/admin)

### Using Docker Directly

```bash
# Build the image
docker build -t postgres-nl-mcp:latest .

# Run the container
docker run -d \
  -p 5000:5000 \
  -e Postgres__DefaultConnectionString="Host=host.docker.internal;Database=mydb;..." \
  -e Ai__Provider=openai \
  -e Ai__ApiKey=sk-... \
  -e Ai__Model=gpt-4 \
  --name postgres-nl-mcp \
  postgres-nl-mcp:latest

# View logs
docker logs -f postgres-nl-mcp

# Stop and remove
docker stop postgres-nl-mcp
docker rm postgres-nl-mcp
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
      "description": "Analyze and describe PostgreSQL database schema...",
      "inputSchema": {
        "type": "object",
        "properties": {
          "connectionString": { "type": "string" },
          "schemaFilter": { "type": "string" },
          "question": { "type": "string" }
        },
        "required": ["connectionString"]
      }
    },
    ...
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
    "connectionString": "Host=localhost;Database=testdb;...",
    "question": "What tables exist?"
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "tables": [...],
    "views": [...],
    "relationships": [...]
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
    "name": "query_database_data",
    "arguments": {
      "connectionString": "...",
      "query": "Show me all customers"
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
    "data": {...}
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

## Security

### Built-in Security Features

1. **Read-Only by Default**:
   - Only SELECT queries are allowed
   - INSERT, UPDATE, DELETE, DROP blocked by default
   - Configurable via `Security:AllowDataModification`

2. **SQL Injection Prevention**:
   - All queries use parameterized statements where possible
   - Input validation and sanitization
   - Dangerous SQL patterns blocked (semicolons, comments, unions in untrusted input)
   - Query analysis before execution

3. **Rate Limiting**:
   - Configurable requests per minute per IP address
   - Protects against abuse and DoS attacks
   - Can be disabled in development

4. **Query Limits**:
   - Maximum rows per query (default: 10,000)
   - Query timeout enforcement (default: 30 seconds)
   - Automatic LIMIT clause injection

5. **Connection Security**:
   - SSL/TLS support for database connections
   - Connection string encryption in production
   - No credentials logged

6. **Schema Filtering**:
   - Block system schemas (pg_catalog, information_schema)
   - Whitelist/blacklist specific schemas
   - Table-level access control

### Security Best Practices

**Never commit secrets**:
```bash
# ✅ Good: Environment variables
export Ai__ApiKey=sk-...

# ✅ Good: User secrets (development)
dotnet user-secrets set "Ai:ApiKey" "sk-..."

# ✅ Good: Docker secrets (production)
docker secret create openai_key ./openai_key.txt

# ❌ Bad: Hardcode in appsettings.json
"ApiKey": "sk-..."  # DON'T DO THIS!
```

**Connection strings**:
```bash
# ✅ Good: SSL enabled
Host=localhost;Database=mydb;Username=user;Password=pass;SSL Mode=Require

# ✅ Good: Read-only user
Host=localhost;Database=mydb;Username=readonly_user;Password=pass

# ❌ Bad: Superuser credentials
Username=postgres;Password=admin123  # DON'T DO THIS!
```

**Production checklist**:
- [ ] Use SSL/TLS for PostgreSQL connections (`SSL Mode=Require`)
- [ ] Enable rate limiting (`Security__EnableRateLimiting=true`)
- [ ] Set appropriate query timeout (`Security__MaxQueryExecutionSeconds=30`)
- [ ] Configure row limits (`Security__MaxRowsPerQuery=10000`)
- [ ] Use read-only database user or disable data modification
- [ ] Store secrets securely (environment variables, key vaults)
- [ ] Review blocked schemas and tables
- [ ] Enable comprehensive logging (but don't log sensitive data)
- [ ] Set up monitoring and alerting
- [ ] Use HTTPS for API endpoints

## Testing

### Test Categories

**Unit Tests**:
- Service logic and business rules
- SQL generation and validation
- Configuration parsing
- Model validation

**Integration Tests**:
- Database operations (requires PostgreSQL)
- End-to-end MCP tool execution
- AI integration (with mocked responses)

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
dotnet test --filter "FullyQualifiedName~DatabaseSchemaServiceTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~DatabaseSchemaServiceTests.GetTables_ReturnsAllTables"

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
public class DatabaseSchemaServiceTests
{
    [Fact]
    public async Task GetTables_ReturnsAllTables()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetTablesAsync(connectionString);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("SELECT id, name FROM products WHERE active = true")]
    public void ValidateQuery_ValidSelect_ReturnsTrue(string query)
    {
        // Arrange & Act
        var isValid = QueryValidator.IsValidQuery(query);

        // Assert
        Assert.True(isValid);
    }
}
```

## Troubleshooting

### Common Issues

**Issue**: AI features not working
```
Error: "AI features are not configured or disabled"
```
**Solutions**:
- Check `Ai__ApiKey` is set: `dotnet user-secrets list`
- Verify `Ai__Enabled=true` in configuration
- Check logs for AI API errors
- Ensure network access to AI provider API

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
- Change ports in docker-compose.yml or appsettings.json
- Check what's using the port: `lsof -i :5000` (macOS/Linux) or `netstat -ano | findstr :5000` (Windows)
- Stop conflicting services

**Issue**: Docker build fails
```
Error: "failed to compute cache key"
```
**Solutions**:
```bash
docker-compose build --no-cache postgres-nl-mcp
docker-compose up -d
```

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "PostgresNaturalLanguageMcp": "Trace",
      "Microsoft.AspNetCore": "Warning"
    },
    "LogQueries": true,
    "LogResults": false,  // Be careful with sensitive data
    "LogAiInteractions": true
  }
}
```

Or via environment variables:

```bash
export Logging__LogLevel__Default=Debug
export Logging__LogQueries=true
export Logging__LogAiInteractions=true
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
      "connectionString": "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres123",
      "question": "What tables exist?"
    }
  }' | jq

# Test query_database_data
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database_data",
    "arguments": {
      "connectionString": "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres123",
      "query": "Show me all customers with their order counts"
    }
  }' | jq
```

## Working with Claude

### When Adding Features

1. **Read the code first**: Use the Read tool to understand existing implementation
2. **Follow .NET conventions**: Use standard .NET/C# patterns and idioms
3. **Update tests**: Add xUnit tests for new functionality
4. **Update documentation**: Update both README.md and this CLAUDE.md
5. **Test thoroughly**: Run tests, build, and test locally with Docker

### When Debugging

1. **Check logs**: Look at Serilog output in console or log files
2. **Verify configuration**: Check user secrets, environment variables, appsettings.json
3. **Test endpoints**: Use curl, Postman, or the Scalar UI
4. **Review code**: Check for common .NET issues (null references, async/await, disposal)
5. **Enable debug mode**: Set `ASPNETCORE_ENVIRONMENT=Development` and logging to Debug

### When Refactoring

1. **Maintain compatibility**: Don't break the MCP protocol API
2. **Update tests**: Ensure all tests pass after changes
3. **Follow patterns**: Maintain consistency with ASP.NET Core best practices
4. **Document changes**: Update XML documentation comments

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

### Testing

- Write unit tests for all services
- Use xUnit framework
- Follow AAA pattern (Arrange, Act, Assert)
- Use [Fact] for single tests, [Theory] for parameterized tests
- Mock external dependencies (databases, AI APIs)
- Aim for >80% code coverage

### Documentation

- Add XML documentation comments (///) for public APIs
- Include <summary>, <param>, <returns>, <exception>
- Document non-obvious logic with inline comments
- Keep README.md and CLAUDE.md up to date
- Document configuration options

## Performance Considerations

- Use async/await for all I/O operations
- Implement connection pooling (Npgsql handles this automatically)
- Use efficient queries with proper indexes
- Stream large result sets when possible
- Cache schema information (future enhancement)
- Profile and optimize hot paths
- Monitor database query performance

## Future Enhancements

- [ ] Query caching for repeated queries
- [ ] GraphQL endpoint alongside REST
- [ ] Support for multiple databases (MySQL, SQL Server)
- [ ] Query builder UI
- [ ] Saved queries and templates
- [ ] Query history and auditing
- [ ] Batch query execution
- [ ] Query scheduling
- [ ] Data export formats (CSV, Excel, JSON)
- [ ] Authentication and authorization
- [ ] Multi-tenant support
- [ ] Query cost estimation
- [ ] Real-time query monitoring

## Related Documentation

- **Main Repository Context**: `/CLAUDE.md` - Repository-wide guidelines and patterns
- **User Documentation**: `README.md` - User-facing documentation
- **Configuration Files**: `appsettings.json`, `appsettings.Development.json`
- **Docker Setup**: `docker-compose.yml`, `Dockerfile`
- **Sample Data**: `docker/init.sql`

## Questions for Claude

When working on the PostgreSQL Natural Language MCP Server, you can ask:

- "How does the SQL generation service work?"
- "What AI providers are supported and how do I add a new one?"
- "How is SQL injection prevented?"
- "Where should I add a new MCP tool?"
- "How do I test with a local database?"
- "How does rate limiting work?"
- "How are foreign key relationships detected?"
- "Where should I add a new configuration option?"
- "How does the MCP protocol implementation work?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for the PostgreSQL Natural Language MCP Server project.
