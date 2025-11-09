# PostgreSQL Natural Language MCP Server

A production-ready Model Context Protocol (MCP) server for PostgreSQL database operations with AI-powered query generation and natural language understanding.

## Features

### 🔍 Tool 1: scan_database_structure
Comprehensive database schema analysis with AI-powered insights.

**Capabilities**:
- List all tables, views, and their relationships
- Display detailed column information (types, constraints, defaults)
- Show primary keys, foreign keys, and indexes
- Identify table relationships and cardinality
- Answer natural language questions about schema
- Provide statistics (row counts, table sizes)

**Example Use Cases**:
- "What tables have foreign keys to the customers table?"
- "Show me all tables in the public schema"
- "What is the structure of the orders table?"

### 💬 Tool 2: query_database_data
Intelligent data querying with automatic relationship detection.

**Capabilities**:
- Convert natural language to SQL queries
- Automatically follow foreign key relationships
- Return structured JSON results
- Include execution metadata and timing
- Handle complex multi-table queries

**Example Use Cases**:
- "Show me all customers who made orders in the last 30 days"
- "Get the top 10 products by revenue"
- "List all orders with customer names and addresses"

### 🤖 Tool 3: advanced_sql_query
AI-powered SQL generation with validation and optimization.

**Capabilities**:
- Generate SQL from detailed natural language descriptions
- Validate query safety (prevent injection, data modification)
- Optimize queries for performance
- Provide query explanations and confidence scores
- Support complex aggregations, joins, and subqueries

**Example Use Cases**:
- "Calculate average order value by customer segment for Q4 2024, showing only segments with more than 100 orders"
- "Find customers who haven't ordered in 90 days but had more than 5 orders in their lifetime"
- "Analyze monthly sales trends with year-over-year comparison"

## Supported LLM Providers

The PostgreSQL Natural Language MCP Server supports multiple LLM providers for AI-powered query generation:

### Cloud Providers

- **OpenAI** (default)
  - Models: `gpt-4`, `gpt-4-turbo-preview`, `gpt-3.5-turbo`
  - Requires: API key from https://platform.openai.com

- **Anthropic Claude**
  - Models: `claude-3-5-sonnet-20241022`, `claude-3-opus-20240229`, `claude-3-sonnet-20240229`
  - Requires: API key from https://console.anthropic.com

- **Google Gemini**
  - Models: `gemini-1.5-pro`, `gemini-1.5-flash`
  - Requires: API key from https://makersuite.google.com/app/apikey

- **Azure OpenAI**
  - Models: Deployment-specific
  - Requires: Azure OpenAI endpoint and API key

### Local Providers

- **Ollama** (local, privacy-friendly)
  - Models: `llama2`, `llama3`, `mistral`, `codellama`, etc.
  - Requires: Ollama running locally (https://ollama.ai)
  - Default endpoint: http://localhost:11434

- **LM Studio** (local, easy setup)
  - Models: Any model loaded in LM Studio
  - Requires: LM Studio running with local server enabled
  - Default endpoint: http://localhost:1234

To configure a provider, set the `Ai__Provider` environment variable to one of: `openai`, `anthropic`, `gemini`, `ollama`, `lmstudio`, or `azureopenai`.

## Quick Start

### Using Docker Compose (Recommended)

1. **Start all services**:
   ```bash
   cd mcp-servers/postgres-nl-mcp
   docker-compose up -d
   ```

2. **Access the services**:
   - **MCP Server API**: http://localhost:5000
   - **API Documentation**: http://localhost:5000/scalar/v1
   - **PostgreSQL**: localhost:5432 (postgres/postgres123)
   - **pgAdmin**: http://localhost:8080 (admin@admin.com/admin)

3. **Test the API**:
   ```bash
   curl http://localhost:5000/mcp/tools
   ```

### Local Development

1. **Prerequisites**:
   - .NET 9 SDK
   - PostgreSQL 16+
   - LLM API key (optional, for AI features) - OpenAI, Anthropic, Gemini, or Azure OpenAI
   - OR local LLM (Ollama or LM Studio) for privacy-focused deployment

2. **Configure the application**:
   ```bash
   cd src/PostgresNaturalLanguageMcp
   dotnet user-secrets init

   # For OpenAI (default)
   dotnet user-secrets set "Ai:Provider" "openai"
   dotnet user-secrets set "Ai:ApiKey" "sk-your-openai-api-key"
   dotnet user-secrets set "Ai:Model" "gpt-4"

   # OR for Anthropic Claude
   # dotnet user-secrets set "Ai:Provider" "anthropic"
   # dotnet user-secrets set "Ai:ApiKey" "sk-ant-your-anthropic-key"
   # dotnet user-secrets set "Ai:Model" "claude-3-5-sonnet-20241022"

   # OR for Google Gemini
   # dotnet user-secrets set "Ai:Provider" "gemini"
   # dotnet user-secrets set "Ai:ApiKey" "AIza-your-gemini-key"
   # dotnet user-secrets set "Ai:Model" "gemini-1.5-pro"

   # OR for Ollama (local)
   # dotnet user-secrets set "Ai:Provider" "ollama"
   # dotnet user-secrets set "Ai:Model" "llama3"
   # dotnet user-secrets set "Ai:BaseUrl" "http://localhost:11434"

   # PostgreSQL connection
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

# AI Configuration
# Choose your LLM provider: openai, anthropic, gemini, ollama, lmstudio, azureopenai
Ai__Provider="openai"
Ai__ApiKey="sk-..."
Ai__Model="gpt-4"
Ai__Enabled=true
Ai__MaxTokens=2000
Ai__Temperature=0.1

# For Azure OpenAI (use Ai__Provider="azureopenai")
Ai__AzureEndpoint="https://your-resource.openai.azure.com"
Ai__AzureDeploymentName="gpt-4"

# For Anthropic Claude (use Ai__Provider="anthropic")
# Ai__ApiKey="sk-ant-..."
# Ai__Model="claude-3-5-sonnet-20241022"

# For Google Gemini (use Ai__Provider="gemini")
# Ai__ApiKey="AIza..."
# Ai__Model="gemini-1.5-pro"

# For Ollama (local LLM, use Ai__Provider="ollama")
# Ai__Model="llama3"
# Ai__BaseUrl="http://localhost:11434"

# For LM Studio (local LLM, use Ai__Provider="lmstudio")
# Ai__Model="local-model"
# Ai__BaseUrl="http://localhost:1234"

# Security Settings
Security__EnableRateLimiting=true
Security__RequestsPerMinute=60
Security__MaxRowsPerQuery=10000
Security__MaxQueryExecutionSeconds=30
Security__AllowDataModification=false
Security__AllowSchemaModification=false
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
  "Ai": {
    "Provider": "openai",
    "ApiKey": null,
    "Model": "gpt-4",
    "BaseUrl": null,
    "AzureEndpoint": null,
    "AzureDeploymentName": null,
    "Enabled": true,
    "MaxTokens": 2000,
    "Temperature": 0.1
  },
  "Security": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "MaxRowsPerQuery": 10000,
    "AllowDataModification": false
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
      "description": "Analyze and describe PostgreSQL database schema...",
      "inputSchema": { ... }
    },
    {
      "name": "query_database_data",
      "description": "Query and analyze data from PostgreSQL tables...",
      "inputSchema": { ... }
    },
    {
      "name": "advanced_sql_query",
      "description": "Generate and execute optimized SQL queries...",
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
    "connectionString": "Host=localhost;Database=testdb;Username=postgres;Password=pass",
    "schemaFilter": "public",
    "question": "What tables exist in the database?"
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "tables": [ ... ],
    "views": [ ... ],
    "relationships": [ ... ],
    "serverVersion": "16.1"
  },
  "metadata": {
    "executedAt": "2024-11-04T10:30:00Z",
    "tableCount": 15,
    "serverVersion": "16.1"
  }
}
```

### Examples

#### Example 1: Schema Analysis

```bash
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "scan_database_structure",
    "arguments": {
      "connectionString": "Host=postgres;Database=testdb;Username=postgres;Password=postgres123",
      "question": "Show me all foreign key relationships"
    }
  }'
```

#### Example 2: Natural Language Query

```bash
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database_data",
    "arguments": {
      "connectionString": "Host=postgres;Database=testdb;Username=postgres;Password=postgres123",
      "query": "Show me the top 5 customers by total order value with their email addresses"
    }
  }'
```

#### Example 3: Advanced SQL Generation

```bash
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "advanced_sql_query",
    "arguments": {
      "connectionString": "Host=postgres;Database=testdb;Username=postgres;Password=postgres123",
      "naturalLanguageQuery": "Calculate the monthly revenue trend for the last 12 months, grouped by product category, including percentage change month-over-month"
    }
  }'
```

## Docker Deployment

### Build and Run

```bash
# Build the image
docker build -t postgres-nl-mcp:latest .

# Run with environment variables
docker run -d \
  -p 5000:5000 \
  -e Postgres__DefaultConnectionString="Host=host.docker.internal;Database=mydb;..." \
  -e Ai__ApiKey="sk-..." \
  --name postgres-nl-mcp \
  postgres-nl-mcp:latest

# View logs
docker logs -f postgres-nl-mcp

# Stop and remove
docker stop postgres-nl-mcp
docker rm postgres-nl-mcp
```

### Docker Compose

**Full stack with PostgreSQL and pgAdmin**:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

**Services**:
- **postgres-nl-mcp**: The MCP server (port 5000)
- **postgres**: PostgreSQL database (port 5432)
- **pgadmin**: Database management UI (port 8080)

### Sample Data

The Docker setup includes sample e-commerce data:
- Customers table (5 sample customers)
- Products table (10 sample products)
- Orders table (6 sample orders)
- Order items table (linked to orders)
- order_summary view

## Security

### Built-in Security Features

1. **Read-Only by Default**
   - Only SELECT queries allowed
   - No INSERT, UPDATE, DELETE, or schema modifications
   - Configurable via `Security:AllowDataModification`

2. **SQL Injection Prevention**
   - All queries use parameterized statements
   - Input validation and sanitization
   - Dangerous function blocking

3. **Rate Limiting**
   - Configurable requests per minute per IP
   - Protects against abuse and DoS
   - Disable in development for convenience

4. **Query Limits**
   - Maximum rows per query (default: 10,000)
   - Query timeout enforcement (default: 30s)
   - Automatic LIMIT clause injection

5. **Schema Filtering**
   - Block system schemas (pg_catalog, information_schema)
   - Whitelist/blacklist specific schemas
   - Table-level filtering support

### Best Practices

**Connection Strings**:
```bash
# ✅ Good: Use environment variables
export Postgres__DefaultConnectionString="Host=..."

# ✅ Good: Use user secrets in development
dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=..."

# ❌ Bad: Hardcode in code or config files
"DefaultConnectionString": "Host=localhost;Password=mypass"
```

**API Keys**:
```bash
# ✅ Good: Environment variable
export Ai__ApiKey="sk-..."

# ✅ Good: User secrets
dotnet user-secrets set "Ai:ApiKey" "sk-..."

# ✅ Good: Docker secrets
docker secret create openai_key ./openai_key.txt

# ❌ Bad: Commit to version control
```

**Production Checklist**:
- [ ] Use SSL/TLS for PostgreSQL connections
- [ ] Enable rate limiting
- [ ] Set appropriate query timeouts
- [ ] Configure row limits
- [ ] Use secure connection string storage
- [ ] Review blocked schemas/tables
- [ ] Enable comprehensive logging
- [ ] Set up monitoring and alerting

## Testing

### Run All Tests

```bash
dotnet test
```

### Run with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

### Run Specific Tests

```bash
# Run tests from specific class
dotnet test --filter "FullyQualifiedName~McpControllerTests"

# Run tests by name pattern
dotnet test --filter "DisplayName~Validation"

# Run in watch mode (auto-run on file changes)
dotnet watch test
```

### Test Categories

- **Unit Tests**: Service logic and business rules
- **Integration Tests**: Database operations (requires PostgreSQL)
- **Controller Tests**: API endpoint behavior

## Troubleshooting

### Common Issues

**Issue**: AI features not working
```
Error: "AI features are not configured or disabled"
```
**Solution**: Set `Ai__ApiKey` and ensure `Ai__Enabled=true`

**Issue**: Database connection fails
```
Error: "Connection refused" or "Authentication failed"
```
**Solution**: Verify connection string, PostgreSQL is running, and credentials are correct

**Issue**: Rate limit exceeded
```
HTTP 429: Too Many Requests
```
**Solution**: Wait or adjust `Security__RequestsPerMinute`, or disable in development

**Issue**: Query timeout
```
Error: "Query execution timed out"
```
**Solution**: Optimize query or increase `Security__MaxQueryExecutionSeconds`

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "PostgresNaturalLanguageMcp": "Trace"
    },
    "LogQueries": true,
    "LogResults": true,
    "LogAiInteractions": true
  }
}
```

### Health Check

```bash
# Check if server is running
curl http://localhost:5000/health

# Expected response
{
  "status": "healthy",
  "timestamp": "2024-11-04T10:30:00Z",
  "version": "1.0.0"
}
```

## Architecture

### Technology Stack
- **.NET 9**: Latest .NET runtime with C# 13
- **ASP.NET Core**: High-performance web framework
- **Npgsql**: PostgreSQL ADO.NET provider
- **Semantic Kernel**: AI/LLM orchestration
- **Serilog**: Structured logging
- **xUnit**: Unit testing framework

### Project Structure
```
src/
├── PostgresNaturalLanguageMcp/
│   ├── Controllers/          # API endpoints
│   │   └── McpController.cs  # MCP tool implementation
│   ├── Services/             # Business logic
│   │   ├── DatabaseSchemaService.cs
│   │   ├── QueryService.cs
│   │   └── SqlGenerationService.cs
│   ├── Models/               # Data models
│   │   ├── McpModels.cs
│   │   ├── DatabaseModels.cs
│   │   └── ConfigurationModels.cs
│   └── Program.cs            # Application startup
└── PostgresNaturalLanguageMcp.Tests/        # Unit tests
```

### Design Patterns
- **Dependency Injection**: All services registered in DI container
- **Options Pattern**: Configuration via strongly-typed options
- **Repository Pattern**: Database access abstraction
- **Strategy Pattern**: Different AI providers (OpenAI, Azure)

## Performance

### Optimizations
- Connection pooling (Npgsql built-in)
- Async/await throughout for non-blocking I/O
- Efficient query execution with proper indexes
- Result streaming for large datasets
- Query caching (planned)

### Benchmarks
On a typical development machine:
- Schema scan (50 tables): ~200-500ms
- Simple query: ~10-50ms
- Complex query with AI: ~1-3s (depends on AI API latency)
- API response time (excluding query): <10ms

## Contributing

See the main repository [CLAUDE.md](../../CLAUDE.md) for detailed contribution guidelines.

Quick checklist:
- Follow existing code patterns
- Write unit tests for new features
- Update documentation
- Run `dotnet format` before committing
- Ensure all tests pass

## License

MIT License - see LICENSE file in repository root

## Support

- **Documentation**: [CLAUDE.md](../../CLAUDE.md)
- **Issues**: [GitHub Issues](https://github.com/yourusername/ai-tools/issues)
- **Discussions**: Repository discussions

---

**Version**: 1.0.0
**Last Updated**: 2025-11-06
