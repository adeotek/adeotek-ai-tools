# CLAUDE.md - AI Tools Repository Context

This document provides comprehensive context for Claude (or any AI assistant) when interacting with this repository. It explains the structure, patterns, and guidelines for development.

## Repository Purpose

This repository is a curated collection of AI-related tools, agents, and Model Context Protocol (MCP) servers. It serves as a central hub for building and deploying AI-powered utilities that can be used independently or integrated into larger AI systems.

### Key Objectives
- **MCP Servers**: Implementations of the Model Context Protocol for various data sources and operations
- **AI Agents**: Autonomous agents for specific tasks (future)
- **AI Tools**: Utility tools and libraries for AI development (future)
- **Production Ready**: All components are production-grade with proper testing, documentation, and security

## Project Structure

```
/
├── README.md                    # Repository overview and quick start
├── CLAUDE.md                    # This file - detailed context for AI interactions
├── mcp-servers/                 # MCP server implementations
│   └── postgres-mcp/           # PostgreSQL MCP Server
│       ├── src/
│       │   ├── PostgresMcp/            # Main application
│       │   │   ├── Program.cs          # Application entry point
│       │   │   ├── PostgresMcp.csproj  # Project file
│       │   │   ├── Controllers/        # API controllers
│       │   │   │   └── McpController.cs # MCP endpoint implementation
│       │   │   ├── Services/           # Business logic services
│       │   │   │   ├── DatabaseSchemaService.cs    # Schema scanning
│       │   │   │   ├── QueryService.cs             # Data querying
│       │   │   │   └── SqlGenerationService.cs     # AI-powered SQL generation
│       │   │   └── Models/             # Data models and DTOs
│       │   │       ├── McpModels.cs           # MCP protocol models
│       │   │       ├── DatabaseModels.cs      # Database schema models
│       │   │       └── ConfigurationModels.cs # Configuration options
│       │   └── PostgresMcp.Tests/      # Unit tests
│       ├── Dockerfile                  # Container definition
│       ├── docker-compose.yml          # Multi-container orchestration
│       ├── docker-init/                # Database initialization scripts
│       └── README.md                   # Project-specific documentation
├── agents/                      # Future: AI agents
└── tools/                       # Future: AI utility tools
```

## Architecture Decisions

### 1. MCP Server Design Pattern

All MCP servers follow a consistent architecture:

**Layered Architecture**:
- **Controllers**: Handle HTTP requests and MCP protocol compliance
- **Services**: Contain business logic and data access
- **Models**: Define data structures and configurations

**Key Principles**:
- Dependency injection for all services
- Separation of concerns (schema, query, SQL generation)
- Async/await throughout for better performance
- Comprehensive error handling and logging

### 2. PostgreSQL MCP Server

**Technology Stack**:
- **.NET 9**: Latest .NET runtime with C# 13 features
- **ASP.NET Core**: Web framework with Minimal APIs
- **Npgsql**: PostgreSQL data provider
- **Semantic Kernel**: AI/LLM integration
- **Serilog**: Structured logging
- **xUnit**: Unit testing framework

**Three Core Tools**:

1. **scan_database_structure**
   - Scans database schema (tables, columns, relationships)
   - Provides detailed metadata (indexes, constraints, foreign keys)
   - Answers natural language questions about schema
   - Uses AI to interpret schema questions

2. **query_database_data**
   - Converts natural language to SQL queries
   - Automatically follows foreign key relationships
   - Returns structured JSON results
   - Includes execution metadata

3. **advanced_sql_query**
   - AI-powered SQL generation from natural language
   - Validates query safety (prevents injection, data modification)
   - Optimizes queries for performance
   - Returns query explanation and confidence score

**Security Features**:
- Read-only by default (no INSERT/UPDATE/DELETE)
- SQL injection prevention via parameterized queries
- Rate limiting per IP address
- Query timeout enforcement
- Row count limits
- Schema/table filtering

### 3. Configuration Management

**Configuration Sources** (in order of precedence):
1. Environment variables
2. appsettings.{Environment}.json
3. appsettings.json
4. User secrets (for development)

**Key Configuration Sections**:
- `Postgres`: Database connection settings
- `Ai`: OpenAI/Azure OpenAI configuration
- `Security`: Safety limits and restrictions
- `Logging`: Log levels and destinations

**Example Connection String**:
```
Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass;SSL Mode=Require
```

### 4. Docker Deployment

**Multi-Stage Build**:
1. Build stage: Compile and build application
2. Publish stage: Create optimized release
3. Runtime stage: Minimal runtime image

**Docker Compose Services**:
- `postgres-mcp`: The MCP server application
- `postgres`: PostgreSQL database for testing
- `pgadmin`: Database management UI (optional)

**Environment Variables for Docker**:
```bash
ASPNETCORE_ENVIRONMENT=Development
Postgres__DefaultConnectionString=Host=postgres;Port=5432;Database=testdb;...
Ai__ApiKey=your-openai-api-key
Ai__Model=gpt-4
Security__EnableRateLimiting=false
```

## Development Guidelines

### Adding a New MCP Server

1. **Create Project Structure**:
   ```bash
   mkdir -p mcp-servers/{name}-mcp/src/{Name}Mcp
   cd mcp-servers/{name}-mcp/src/{Name}Mcp
   dotnet new webapi -n {Name}Mcp
   ```

2. **Follow Naming Conventions**:
   - Project: `{Name}Mcp` (PascalCase)
   - Namespace: `{Name}Mcp`
   - Controllers: `{Name}Controller.cs`
   - Services: `I{Name}Service.cs` and `{Name}Service.cs`

3. **Implement MCP Protocol**:
   - Create models for MCP requests/responses
   - Implement `/mcp/tools` endpoint (tool discovery)
   - Implement `/mcp/tools/call` endpoint (tool execution)
   - Implement `/mcp/jsonrpc` endpoint (JSON-RPC 2.0)

4. **Add Documentation**:
   - Project-specific README.md
   - API documentation with Swagger
   - XML documentation comments in code

5. **Create Tests**:
   - Unit tests for services
   - Integration tests for controllers
   - Aim for >80% code coverage

### Code Quality Standards

**Required Practices**:
- ✅ Use latest C# features (records, pattern matching, etc.)
- ✅ Async/await for all I/O operations
- ✅ Dependency injection for all services
- ✅ XML documentation comments for public APIs
- ✅ Comprehensive error handling with proper exceptions
- ✅ Structured logging with Serilog
- ✅ Unit tests with xUnit and Moq
- ✅ Follow .NET naming conventions
- ✅ Use nullable reference types

**Avoid**:
- ❌ Hardcoded connection strings or secrets
- ❌ Blocking I/O operations (use async)
- ❌ String concatenation for SQL (use parameterized queries)
- ❌ Exposing implementation details in public APIs
- ❌ Insufficient error handling

### Security Considerations

**Always**:
- Validate and sanitize all user inputs
- Use parameterized queries (never string concatenation)
- Implement rate limiting for public APIs
- Set appropriate timeouts for operations
- Use SSL/TLS for database connections
- Follow principle of least privilege
- Log security-relevant events

**Never**:
- Commit secrets or API keys to version control
- Allow arbitrary SQL execution without validation
- Expose internal error details to clients
- Trust client-provided data without validation

## Common Commands and Workflows

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet run

# Run with specific environment
dotnet run --environment Development

# Watch mode (auto-reload on changes)
dotnet watch run
```

### Docker Deployment

```bash
# Build and start all services
cd mcp-servers/postgres-mcp
docker-compose up -d

# View logs
docker-compose logs -f postgres-mcp

# Stop services
docker-compose down

# Rebuild after code changes
docker-compose up -d --build

# Run with specific environment file
docker-compose --env-file .env.production up -d
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Run specific test
dotnet test --filter "FullyQualifiedName~McpControllerTests"

# Run tests in watch mode
dotnet watch test
```

### Database Operations

```bash
# Connect to PostgreSQL in Docker
docker exec -it postgres-mcp-db psql -U postgres -d testdb

# Run SQL script
docker exec -i postgres-mcp-db psql -U postgres -d testdb < script.sql

# Backup database
docker exec postgres-mcp-db pg_dump -U postgres testdb > backup.sql

# Restore database
docker exec -i postgres-mcp-db psql -U postgres testdb < backup.sql
```

## Interacting with PostgreSQL MCP Server

### API Endpoints

**List Available Tools**:
```http
GET http://localhost:5000/mcp/tools
```

**Call a Tool**:
```http
POST http://localhost:5000/mcp/tools/call
Content-Type: application/json

{
  "name": "scan_database_structure",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass",
    "schemaFilter": "public"
  }
}
```

**JSON-RPC Endpoint**:
```http
POST http://localhost:5000/mcp/jsonrpc
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/list"
}
```

### Example Tool Calls

**1. Scan Database Structure**:
```json
{
  "name": "scan_database_structure",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=postgres",
    "question": "What tables have foreign keys to the customers table?"
  }
}
```

**2. Query Database Data**:
```json
{
  "name": "query_database_data",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=postgres",
    "query": "Show me all orders from the last 7 days with customer names and total amounts"
  }
}
```

**3. Advanced SQL Query**:
```json
{
  "name": "advanced_sql_query",
  "arguments": {
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=postgres",
    "naturalLanguageQuery": "Calculate the average order value by product category for customers who made more than 3 orders"
  }
}
```

## Configuration Examples

### Setting Up OpenAI API

**Via Environment Variables**:
```bash
export Ai__ApiKey="sk-..."
export Ai__Model="gpt-4"
export Ai__Enabled="true"
```

**Via appsettings.json**:
```json
{
  "Ai": {
    "ApiKey": "sk-...",
    "Model": "gpt-4",
    "Enabled": true
  }
}
```

**Via User Secrets** (recommended for development):
```bash
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet user-secrets init
dotnet user-secrets set "Ai:ApiKey" "sk-..."
```

### Using Azure OpenAI

```json
{
  "Ai": {
    "ApiKey": "your-azure-key",
    "AzureEndpoint": "https://your-resource.openai.azure.com",
    "AzureDeploymentName": "gpt-4",
    "Model": "gpt-4",
    "Enabled": true
  }
}
```

### Security Configuration

**Production Settings**:
```json
{
  "Security": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "AllowedSchemas": ["public", "app"],
    "BlockedSchemas": ["pg_catalog", "information_schema", "pg_temp"],
    "MaxRowsPerQuery": 10000,
    "MaxQueryExecutionSeconds": 30,
    "AllowDataModification": false,
    "AllowSchemaModification": false
  }
}
```

## Troubleshooting

### Common Issues

**1. AI Features Not Working**:
- Check if `Ai__ApiKey` is set
- Verify `Ai__Enabled` is `true`
- Check logs for API errors
- Ensure network access to OpenAI API

**2. Database Connection Fails**:
- Verify connection string format
- Check if PostgreSQL is running
- Ensure network connectivity
- Verify credentials

**3. Rate Limiting Issues**:
- Check `Security__RequestsPerMinute` setting
- Clear rate limit cache: restart application
- Disable rate limiting in development

**4. Docker Container Issues**:
- Check logs: `docker-compose logs`
- Verify environment variables
- Ensure ports are not in use
- Check Docker network connectivity

### Debugging

**Enable Debug Logging**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Check Health Status**:
```bash
curl http://localhost:5000/health
```

**View Swagger Documentation**:
```
http://localhost:5000/swagger
```

## Future Enhancements

### Planned Features
- [ ] Support for multiple database types (MySQL, SQL Server, MongoDB)
- [ ] GraphQL endpoint for flexible querying
- [ ] WebSocket support for real-time updates
- [ ] Query caching and optimization
- [ ] Built-in data visualization
- [ ] Export to various formats (CSV, Excel, JSON)
- [ ] Scheduled query execution
- [ ] Query history and favorites

### Potential AI Agents
- Database optimization agent
- Schema migration assistant
- Data quality analyzer
- Anomaly detection agent

### Additional MCP Servers
- MongoDB MCP Server
- Redis MCP Server
- Elasticsearch MCP Server
- REST API MCP Server
- File System MCP Server

## Contributing Guidelines

When contributing to this repository:

1. **Follow the established patterns** in existing code
2. **Write tests** for all new functionality
3. **Update documentation** including this file if adding new patterns
4. **Use semantic commits**: `feat:`, `fix:`, `docs:`, `refactor:`, etc.
5. **Ensure security**: No hardcoded secrets, proper validation
6. **Run tests** before committing: `dotnet test`
7. **Format code**: Use `dotnet format`

## Additional Resources

- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

## Contact and Support

For questions, issues, or contributions:
- Open an issue on GitHub
- Follow the project guidelines
- Join discussions in the repository

---

**Last Updated**: 2025-11-04
**Document Version**: 1.0.0
