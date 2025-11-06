# Adeotek AI Tools - Context for Claude

This document provides comprehensive context about the Adeotek AI Tools repository for Claude (both CLI and Web) interactions. It describes the repository structure, purpose, development guidelines, and how to work with the various AI tools, agents, and MCP servers contained within.

## Repository Overview

**Purpose**: A collection of AI-related tools, intelligent agents, and Model Context Protocol (MCP) servers designed to enhance productivity and enable AI-powered workflows.

**Organization**: The repository is structured to accommodate multiple types of AI tools:
- **MCP Servers**: Protocol-compliant servers for database operations and other services
- **Agents**: Intelligent agents that use LLMs to accomplish specific tasks
- **Tools**: Utility tools and helpers for AI-powered workflows

## Project Structure

```
adeotek-ai-tools/
├── README.md                 # High-level repository overview
├── CLAUDE.md                # This file - detailed context for Claude
├── LICENSE                  # MIT License
├── mcp-servers/             # Model Context Protocol servers
│   └── postgres-mcp/        # PostgreSQL MCP server (planned)
├── agents/                  # Intelligent AI agents
│   └── http-agent/          # Intelligent HTTP request agent
│       ├── cmd/
│       │   └── server/      # Main application entry
│       ├── internal/        # Internal packages
│       │   ├── agent/       # Core agent logic
│       │   ├── handlers/    # HTTP handlers & web UI
│       │   └── models/      # Data models
│       ├── config/          # Configuration files
│       ├── Dockerfile       # Docker build
│       ├── docker-compose.yml
│       └── README.md        # Agent-specific documentation
└── tools/                   # Additional AI tools (planned)
```

## Current Projects

### 1. Intelligent HTTP Agent (`/agents/http-agent`)

**Technology Stack**: Go 1.24.7+, Gin web framework, Multiple LLM providers

**Purpose**: An AI-powered HTTP request tool that acts as an intelligent `curl` alternative. It makes HTTP/HTTPS requests and provides natural language analysis of the results.

**Key Features**:
- Natural language interface for HTTP requests
- Web UI for easy interaction
- Support for all HTTP methods (GET, POST, PUT, DELETE, etc.)
- AI-powered response analysis using multiple LLM providers
- **DNS diagnostics**: Automatic hostname resolution, IP lookup, and timing (nslookup-like)
- **SSL certificate inspection**: Certificate validation, expiration, CA details, and algorithm information
- **Configurable SSL verification**: Per-request SSL verification toggle for self-signed certificates
- JSON formatting, status code interpretation, performance analysis
- Built-in security: SSRF protection, configurable SSL verification, private IP blocking
- Docker deployment ready

**Supported LLM Providers**:
- **OpenAI**: GPT-4, GPT-4o, GPT-3.5-turbo
- **Anthropic**: Claude 3.5 Sonnet, Claude 3 Opus, Claude 3 Sonnet
- **Google Gemini**: Gemini 1.5 Pro, Gemini 1.5 Flash
- **Ollama**: Local models (Llama 2, Llama 3, Mistral, CodeLlama, etc.)
- **LM Studio**: Any locally loaded model via OpenAI-compatible API

**Configuration**:
- Primary: Environment variables (`OPENAI_API_KEY`, `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`, `LLM_PROVIDER`, etc.)
- Alternative: YAML config file (`config/config.yaml`)
- See `agents/http-agent/README.md` for detailed configuration options for each provider

**Running Locally**:
```bash
cd agents/http-agent
export OPENAI_API_KEY=your-key
go run cmd/server/main.go
# Open http://localhost:8080
```

**Running with Docker**:
```bash
cd agents/http-agent
cp .env.example .env
# Edit .env with your API key
docker-compose up -d
```

**API Endpoints**:
- `GET /` - Web UI
- `POST /api/request` - Execute HTTP request with AI analysis
- `GET /health` - Health check

**Example Use Cases**:
- "Is this API endpoint accessible?"
- "What's the response code and what does it mean?"
- "Show me the JSON response in a readable format"
- "Is this API response time acceptable?"
- "What are the security headers in this response?"

### 2. PostgreSQL MCP Server (`/mcp-servers/postgres-mcp`)

**Status**: ✅ Production Ready

**Technology Stack**: .NET 9, ASP.NET Core, Npgsql, Semantic Kernel, Scalar, Serilog

**Purpose**: A production-ready Model Context Protocol server for PostgreSQL database operations with AI-powered query generation and natural language understanding.

**Key Features**:
- **Three MCP Tools**:
  1. `scan_database_structure` - Comprehensive database schema analysis with AI-powered Q&A
  2. `query_database_data` - Natural language to SQL with automatic relationship detection
  3. `advanced_sql_query` - AI-powered SQL generation with validation and optimization
- **AI-Powered**: Integrates with OpenAI or Azure OpenAI via Semantic Kernel
- **Security First**: SQL injection prevention, rate limiting, read-only by default, query timeouts
- **Production Ready**: Comprehensive testing (xUnit), structured logging (Serilog), error handling
- **API Documentation**: Beautiful Scalar UI for interactive API exploration
- **Docker Ready**: Complete docker-compose setup with PostgreSQL and pgAdmin
- **MCP Compliant**: Full JSON-RPC 2.0 support for Model Context Protocol

**Configuration**:
- Environment variables for all settings (connection strings, API keys, security)
- Support for both OpenAI and Azure OpenAI
- Configurable security limits (max rows, query timeout, rate limiting)
- SSL/TLS support for database connections

**Running Locally**:
```bash
cd mcp-servers/postgres-mcp
# Set OpenAI API key
export Ai__ApiKey=your-openai-key
# Start with Docker Compose (includes PostgreSQL + pgAdmin)
docker-compose up -d
# Access API documentation at http://localhost:5000/scalar/v1
```

**Running for Development**:
```bash
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet user-secrets set "Ai:ApiKey" "your-openai-key"
dotnet restore
dotnet run
# Open http://localhost:5000/scalar/v1
```

**API Endpoints**:
- `GET /mcp/tools` - List available MCP tools
- `POST /mcp/tools/call` - Execute an MCP tool
- `POST /mcp/jsonrpc` - JSON-RPC 2.0 endpoint
- `GET /health` - Health check
- `GET /scalar/v1` - API documentation UI

**Example Use Cases**:
- "What tables have foreign keys to the customers table?"
- "Show me all customers who made orders in the last 30 days"
- "Calculate average order value by product category for customers with more than 3 orders"
- Automated database documentation generation
- Natural language data exploration
- AI-assisted SQL query writing

## Development Guidelines

### Adding a New Agent

1. **Create directory structure**:
   ```bash
   mkdir -p agents/new-agent/{cmd/server,internal/{agent,handlers,models},config}
   ```

2. **Initialize Go module** (for Go agents):
   ```bash
   cd agents/new-agent
   go mod init github.com/adeotek/adeotek-ai-tools/agents/new-agent
   ```

3. **Implement core functionality**:
   - Define models in `internal/models/`
   - Implement agent logic in `internal/agent/`
   - Create handlers in `internal/handlers/`
   - Set up main entry point in `cmd/server/`

4. **Add Docker support**:
   - Create `Dockerfile` with multi-stage build
   - Create `docker-compose.yml` for easy deployment
   - Add `.env.example` with configuration template

5. **Document thoroughly**:
   - Create comprehensive `README.md` in agent directory
   - Update this `CLAUDE.md` with agent details
   - Update root `README.md` with quick reference

### Adding a New MCP Server

1. **Create directory structure** based on the technology:
   - .NET: `mcp-servers/name-mcp/src/NameMcp/`
   - Go: `mcp-servers/name-mcp/cmd/server/`
   - Python: `mcp-servers/name-mcp/src/name_mcp/`

2. **Implement MCP protocol**:
   - Tool discovery endpoint
   - JSON-RPC 2.0 communication
   - Proper schema definitions
   - Error handling and status codes

3. **Add documentation**:
   - Tool descriptions and usage examples
   - Configuration instructions
   - Docker deployment guide

### Code Quality Standards

**Go Projects**:
- Use Go 1.23+ features where appropriate
- Follow standard Go project layout
- Implement dependency injection
- Use structured logging (logrus, zap, or zerolog)
- Add comprehensive error handling
- Write unit tests for core logic
- Use context for cancellation/timeout
- Document exported functions with GoDoc comments
- Format with `gofmt` and lint with `golangci-lint`

**.NET Projects**:
- Use .NET 9 and C# 13
- Follow .NET coding conventions
- Implement dependency injection via built-in DI
- Add XML documentation comments
- Use async/await throughout
- Write unit tests with xUnit
- Use Serilog for structured logging

**All Projects**:
- Never hardcode secrets or API keys
- Implement proper input validation
- Add rate limiting where appropriate
- Support configuration via environment variables
- Include Docker deployment options
- Write comprehensive README files

### Security Considerations

- ✅ Use environment variables for secrets
- ✅ Implement input validation and sanitization
- ✅ Add SSRF protection for network requests
- ✅ Use parameterized queries for database operations
- ✅ Implement request timeouts
- ✅ Add rate limiting for public APIs
- ✅ Support SSL/TLS for all connections
- ✅ Never log sensitive information
- ✅ Use non-root users in Docker containers

## Common Commands

### HTTP Agent

```bash
# Local development
cd agents/http-agent
go run cmd/server/main.go

# Build binary
go build -o http-agent ./cmd/server/main.go

# Run tests
go test ./...

# Format code
go fmt ./...

# Docker deployment
docker-compose up -d
docker-compose logs -f
docker-compose down

# Environment variables - OpenAI
export OPENAI_API_KEY=sk-...
export LLM_PROVIDER=openai
export LLM_MODEL=gpt-4-turbo-preview
export PORT=8080

# Environment variables - Anthropic Claude
export ANTHROPIC_API_KEY=sk-ant-...
export LLM_PROVIDER=anthropic
export LLM_MODEL=claude-3-5-sonnet-20241022

# Environment variables - Google Gemini
export GEMINI_API_KEY=AIza...
export LLM_PROVIDER=gemini
export LLM_MODEL=gemini-1.5-pro

# Environment variables - Ollama (local)
export LLM_PROVIDER=ollama
export LLM_MODEL=llama2
export HTTP_AGENT_LLM_BASE_URL=http://localhost:11434
# Note: Make sure Ollama is running: ollama serve

# Environment variables - LM Studio (local)
export LLM_PROVIDER=lmstudio
export LLM_MODEL=local-model
export HTTP_AGENT_LLM_BASE_URL=http://localhost:1234
```

### PostgreSQL MCP Server

```bash
# Local development
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet restore
dotnet build
dotnet run

# Run tests
cd mcp-servers/postgres-mcp
dotnet test

# Format code
dotnet format

# Docker deployment
cd mcp-servers/postgres-mcp
docker-compose up -d
docker-compose logs -f postgres-mcp
docker-compose down

# Environment variables - OpenAI
export Ai__ApiKey=sk-...
export Ai__Model=gpt-4
export Ai__Enabled=true
export ASPNETCORE_ENVIRONMENT=Development

# Environment variables - Azure OpenAI
export Ai__ApiKey=your-azure-key
export Ai__AzureEndpoint=https://your-resource.openai.azure.com
export Ai__AzureDeploymentName=gpt-4
export Ai__Model=gpt-4

# Database connection
export Postgres__DefaultConnectionString="Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=yourpass"

# Security settings
export Security__EnableRateLimiting=false
export Security__MaxRowsPerQuery=1000
export Security__AllowDataModification=false

# User secrets (recommended for development)
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet user-secrets init
dotnet user-secrets set "Ai:ApiKey" "sk-..."
dotnet user-secrets set "Postgres:DefaultConnectionString" "Host=localhost;..."

# Access API documentation
# Open http://localhost:5000/scalar/v1

# Test API endpoints
curl http://localhost:5000/health
curl http://localhost:5000/mcp/tools
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{"name":"scan_database_structure","arguments":{"connectionString":"Host=postgres;Database=testdb;...","question":"What tables exist?"}}'
```

## Architecture Patterns

### Agent Pattern

Agents in this repository follow a common pattern:

1. **Models Layer** (`internal/models/`): Data structures and configuration
2. **Agent Layer** (`internal/agent/`): Core business logic and AI integration
3. **Handler Layer** (`internal/handlers/`): HTTP handlers and UI
4. **Main Entry Point** (`cmd/server/`): Application initialization and server setup

### Configuration Management

All projects support multiple configuration methods:
1. Environment variables (highest priority)
2. Configuration files (YAML/JSON)
3. Default values (lowest priority)

### LLM Integration

Projects that integrate with LLMs follow this pattern:
- Abstract LLM interface for multiple providers
- Support for multiple cloud and local LLM providers:
  - **Cloud**: OpenAI, Anthropic, Google Gemini
  - **Local**: Ollama, LM Studio
- Configurable models and parameters
- Structured prompts with system and user messages
- Error handling for API failures
- Provider-specific API implementations

## Working with Claude

### When Adding Features

1. **Understand the context**: Read the relevant README and this CLAUDE.md
2. **Follow patterns**: Use existing agents/servers as templates
3. **Test thoroughly**: Build, run, and test your changes
4. **Document**: Update README files and this CLAUDE.md
5. **Security**: Never commit secrets, validate inputs, handle errors

### When Debugging

1. **Check logs**: Look at application logs for errors
2. **Verify configuration**: Ensure environment variables are set
3. **Test endpoints**: Use curl or the web UI to test functionality
4. **Review code**: Check for common issues (nil pointers, missing error handling)

### When Refactoring

1. **Maintain compatibility**: Don't break existing APIs
2. **Update tests**: Ensure tests pass after changes
3. **Update docs**: Keep documentation in sync with code changes
4. **Follow conventions**: Maintain consistent code style

## API Key Management

### For Development

```bash
# Create .env file
cp agents/http-agent/.env.example agents/http-agent/.env

# Edit .env and add your keys
OPENAI_API_KEY=sk-...
```

### For Production

Use secure secret management:
- Kubernetes secrets
- AWS Secrets Manager
- HashiCorp Vault
- Azure Key Vault
- Environment variables in secure deployment platforms

## Testing

### HTTP Agent

```bash
# Test with example request
curl -X POST http://localhost:8080/api/request \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://api.github.com/users/github",
    "method": "GET",
    "prompt": "Tell me about this user"
  }'

# Test health endpoint
curl http://localhost:8080/health
```

### PostgreSQL MCP Server

```bash
# Test health endpoint
curl http://localhost:5000/health

# List available MCP tools
curl http://localhost:5000/mcp/tools

# Test scan_database_structure tool
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "scan_database_structure",
    "arguments": {
      "connectionString": "Host=postgres;Port=5432;Database=testdb;Username=postgres;Password=postgres123",
      "question": "What tables exist in the database?"
    }
  }'

# Test query_database_data tool
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database_data",
    "arguments": {
      "connectionString": "Host=postgres;Port=5432;Database=testdb;Username=postgres;Password=postgres123",
      "query": "Show me all customers with their order counts"
    }
  }'

# Test advanced_sql_query tool
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "advanced_sql_query",
    "arguments": {
      "connectionString": "Host=postgres;Port=5432;Database=testdb;Username=postgres;Password=postgres123",
      "naturalLanguageQuery": "Calculate the average order value by product category"
    }
  }'

# Run unit tests
cd mcp-servers/postgres-mcp
dotnet test --verbosity normal

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

## Deployment

### Docker (Recommended)

Each project includes Docker support:
```bash
cd agents/http-agent
docker-compose up -d
```

### Kubernetes

Example deployment for HTTP Agent:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: http-agent
spec:
  replicas: 2
  selector:
    matchLabels:
      app: http-agent
  template:
    metadata:
      labels:
        app: http-agent
    spec:
      containers:
      - name: http-agent
        image: adeotek/http-agent:latest
        ports:
        - containerPort: 8080
        env:
        - name: OPENAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: llm-secrets
              key: openai-api-key
```

## Troubleshooting

### Common Issues

**HTTP Agent**:
1. **"API key required" errors**: Set `OPENAI_API_KEY` or `ANTHROPIC_API_KEY`
2. **Port conflicts**: Change `PORT` environment variable
3. **Docker build fails**: Clear cache with `docker-compose build --no-cache`
4. **Private IP blocked**: Set `BLOCK_PRIVATE_IPS=false` for local testing
5. **Request timeout**: Increase `HTTP_TIMEOUT` value

**PostgreSQL MCP Server**:
1. **AI features not working**:
   - Check if `Ai__ApiKey` is set
   - Verify `Ai__Enabled=true` in configuration
   - Check logs for API errors
   - Ensure network access to OpenAI API
2. **Database connection fails**:
   - Verify connection string format: `Host=...;Port=5432;Database=...;Username=...;Password=...`
   - Check if PostgreSQL is running
   - Ensure network connectivity (especially in Docker)
   - Verify credentials are correct
3. **Rate limiting issues**:
   - Check `Security__RequestsPerMinute` setting
   - Disable rate limiting in development: `Security__EnableRateLimiting=false`
   - Restart application to clear rate limit cache
4. **Query timeout**:
   - Increase `Security__MaxQueryExecutionSeconds` value
   - Optimize slow queries
   - Check database performance
5. **Port conflicts**:
   - PostgreSQL MCP uses port 5000 (API) and 5001 (HTTPS)
   - pgAdmin uses port 8080
   - PostgreSQL uses port 5432
   - Change ports in docker-compose.yml if needed

### Debug Mode

Enable debug logging:
```bash
# For Go/Gin projects (HTTP Agent)
export GIN_MODE=debug
export LOG_LEVEL=debug

# For .NET projects (PostgreSQL MCP)
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
export Logging__LogQueries=true
export Logging__LogResults=true
```

## Contributing

When contributing to this repository:

1. Create feature branch: `git checkout -b feature/new-feature`
2. Follow code quality standards
3. Add tests for new functionality
4. Update documentation
5. Test Docker deployment
6. Create pull request with detailed description

## Future Roadmap

### Completed Projects
- [x] **HTTP Agent** (Go) - Intelligent HTTP request tool with AI analysis
- [x] **PostgreSQL MCP Server** (.NET 9) - AI-powered database operations

### Planned MCP Servers
- [ ] MySQL MCP Server (.NET 9)
- [ ] MongoDB MCP Server (Go)
- [ ] Redis MCP Server (Go)
- [ ] Elasticsearch MCP Server (Go)

### Planned Agents
- [ ] Document Analysis Agent (Python)
- [ ] Code Review Agent (Go)
- [ ] Email Assistant Agent (Go)
- [ ] Data Transformation Agent (.NET)

### Planned Features
- [ ] Request history and favorites (HTTP Agent)
- [ ] WebSocket support for real-time updates (HTTP Agent)
- [ ] Query caching and optimization (PostgreSQL MCP)
- [ ] GraphQL endpoint (PostgreSQL MCP)
- [ ] Authentication and user management
- [ ] Shared agent library for common functionality
- [ ] CLI tools for all agents
- [ ] Performance monitoring and metrics dashboard
- [ ] Multi-database query aggregation

## Resources

- **Repository**: https://github.com/adeotek/adeotek-ai-tools
- **Issues**: https://github.com/adeotek/adeotek-ai-tools/issues
- **License**: MIT License
- **OpenAI API**: https://platform.openai.com/docs
- **Anthropic API**: https://docs.anthropic.com
- **MCP Specification**: https://modelcontextprotocol.io

## Questions for Claude

When interacting with Claude about this repository, you can ask:

- "How do I add a new agent to the repository?"
- "What's the architecture of the HTTP agent?"
- "How do I configure the LLM provider?"
- "What security measures are in place?"
- "How do I deploy an agent with Docker?"
- "What are the code quality standards?"
- "How do I test the HTTP agent?"
- "What's the roadmap for this repository?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for all projects in this repository.
