# Adeotek AI Tools - Context for Claude

This document provides high-level context about the Adeotek AI Tools repository for Claude (both CLI and Web) interactions. It describes the repository structure, general development guidelines, and architectural patterns shared across all projects.

**For project-specific details, see the CLAUDE.md file in each project directory:**
- **HTTP Agent**: `/agents/http-agent/CLAUDE.md`
- **PostgreSQL MCP Server** (read-only): `/mcp-servers/postgres-mcp/CLAUDE.md`
- **PostgreSQL Natural Language MCP**: `/mcp-servers/postgres-nl-mcp/CLAUDE.md`

## Repository Overview

**Purpose**: A collection of AI-related tools, intelligent agents, and Model Context Protocol (MCP) servers designed to enhance productivity and enable AI-powered workflows.

**Organization**: The repository is structured to accommodate multiple types of AI tools:
- **MCP Servers** (`/mcp-servers/`): Protocol-compliant servers for database operations and other services
- **Agents** (`/agents/`): Intelligent agents that use LLMs to accomplish specific tasks
- **Tools** (`/tools/`): Utility tools and helpers for AI-powered workflows (planned)

## Project Structure

```
adeotek-ai-tools/
├── README.md                 # High-level repository overview
├── CLAUDE.md                # This file - detailed context for Claude
├── LICENSE                  # MIT License
├── mcp-servers/             # Model Context Protocol servers
│   └── postgres-nl-mcp/        # PostgreSQL MCP server (planned)
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

**Purpose**: An AI-powered HTTP request tool that acts as an intelligent `curl` alternative with natural language analysis.

**Key Features**: Natural language interface, Web UI, DNS diagnostics, SSL certificate inspection, multiple LLM providers (OpenAI, Anthropic, Gemini, Ollama, LM Studio), SSRF protection.

**Quick Start**:
```bash
cd agents/http-agent
export OPENAI_API_KEY=your-key
go run cmd/server/main.go
# Open http://localhost:8080
```

**📄 For detailed documentation**: See `/agents/http-agent/CLAUDE.md`

### 2. PostgreSQL MCP Server (`/mcp-servers/postgres-mcp`)

**Technology Stack**: .NET 9, ASP.NET Core, Npgsql, Serilog, Scalar

**Purpose**: A **read-only** Model Context Protocol server for PostgreSQL providing secure, direct SQL access without AI/LLM dependencies.

**Key Features**: Two MCP tools (database structure scanning, read-only queries), comprehensive query validation, no AI required, lightweight and fast.

**Quick Start**:
```bash
cd mcp-servers/postgres-mcp
docker-compose up -d
# Open http://localhost:5000/scalar/v1
```

**📄 For detailed documentation**: See `/mcp-servers/postgres-mcp/CLAUDE.md`

### 3. PostgreSQL Natural Language MCP Server (`/mcp-servers/postgres-nl-mcp`)

**Status**: ✅ Production Ready

**Technology Stack**: .NET 9, ASP.NET Core, Npgsql, Semantic Kernel, Scalar, Serilog

**Purpose**: A production-ready Model Context Protocol server for PostgreSQL with AI-powered query generation.

**Key Features**: Three MCP tools (schema analysis, natural language queries, advanced SQL generation), multiple AI providers, security-first design, comprehensive testing.

**Quick Start**:
```bash
cd mcp-servers/postgres-nl-mcp
export Ai__ApiKey=your-openai-key
docker-compose up -d
# Open http://localhost:5000/scalar/v1
```

**📄 For detailed documentation**: See `/mcp-servers/postgres-nl-mcp/CLAUDE.md`

## Development Guidelines

### Adding a New Agent

1. **Create directory structure**:
   ```bash
   mkdir -p agents/new-agent/{cmd/server,internal/{agent,handlers,models},config}
   ```

2. **Initialize module** (language-specific):
   ```bash
   # For Go agents
   cd agents/new-agent
   go mod init github.com/adeotek/adeotek-ai-tools/agents/new-agent

   # For .NET agents
   dotnet new webapi -n NewAgent
   ```

3. **Implement core functionality**:
   - Define models for data structures
   - Implement agent logic and AI integration
   - Create handlers for HTTP endpoints and UI
   - Set up main entry point

4. **Add Docker support**:
   - Create `Dockerfile` with multi-stage build
   - Create `docker-compose.yml` for easy deployment
   - Add `.env.example` with configuration template

5. **Document thoroughly**:
   - Create comprehensive `README.md` in agent directory (user-facing)
   - Create `CLAUDE.md` in agent directory (context for Claude)
   - Update this root `CLAUDE.md` with a brief project overview
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
   - Create comprehensive `README.md` (user-facing)
   - Create `CLAUDE.md` (context for Claude)
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

## Common Development Commands

For project-specific commands, see the CLAUDE.md file in each project directory. General commands:

**Go Projects** (e.g., HTTP Agent):
```bash
go mod download      # Install dependencies
go run cmd/server/main.go  # Run locally
go build             # Build binary
go test ./...        # Run tests
go fmt ./...         # Format code
```

**.NET Projects** (e.g., PostgreSQL MCP):
```bash
dotnet restore       # Install dependencies
dotnet run           # Run locally
dotnet build         # Build project
dotnet test          # Run tests
dotnet format        # Format code
```

**Docker** (All Projects):
```bash
docker-compose up -d       # Start services
docker-compose logs -f     # View logs
docker-compose down        # Stop services
docker-compose build --no-cache  # Rebuild
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

All projects include comprehensive test suites. For project-specific testing instructions, see each project's CLAUDE.md file.

**General Testing Approach**:
- Unit tests for business logic
- Integration tests for external dependencies (databases, APIs)
- Mock external services in unit tests
- Aim for reasonable test coverage (>70%)
- Test error cases and edge conditions

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

For project-specific troubleshooting, see each project's CLAUDE.md file.

### General Troubleshooting Steps

1. **Check logs**: Look at application logs for error messages
2. **Verify configuration**: Ensure required environment variables are set
3. **Test connectivity**: Verify network access to external services (databases, APIs)
4. **Enable debug mode**: Increase log verbosity for more details
5. **Clear caches**: Rebuild Docker images without cache if needed
6. **Check ports**: Ensure ports aren't already in use

### Common Issues Across Projects

**API Key Issues**:
- Verify the correct environment variable is set for your provider
- Ensure the key is valid and has proper permissions
- Check if the key is being read correctly (don't log it!)

**Docker Issues**:
- Clear build cache: `docker-compose build --no-cache`
- Remove old containers: `docker-compose down -v`
- Check Docker logs: `docker-compose logs -f`

**Port Conflicts**:
- Check what's using the port: `lsof -i :PORT` (macOS/Linux) or `netstat -ano | findstr :PORT` (Windows)
- Change the port in configuration or docker-compose.yml

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
- [x] **PostgreSQL Natural Language MCP Server** (.NET 9) - AI-powered database operations

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

### Repository-Level Questions

When asking about the overall repository structure and guidelines:

- "How do I add a new agent to the repository?"
- "How do I add a new MCP server to the repository?"
- "What are the general code quality standards?"
- "What security measures should I implement?"
- "How do I deploy projects with Docker?"
- "What's the roadmap for this repository?"
- "What are the common architectural patterns?"

### Project-Specific Questions

For questions about specific projects, Claude will automatically reference the appropriate project-specific CLAUDE.md file:

- "How does the HTTP agent handle SSL certificates?" → See `/agents/http-agent/CLAUDE.md`
- "What LLM providers does the HTTP agent support?" → See `/agents/http-agent/CLAUDE.md`
- "How does query validation work in postgres-mcp?" → See `/mcp-servers/postgres-mcp/CLAUDE.md`
- "What's the difference between postgres-mcp and postgres-nl-mcp?" → See `/mcp-servers/postgres-mcp/CLAUDE.md` or `/mcp-servers/postgres-nl-mcp/CLAUDE.md`
- "How do I configure the PostgreSQL Natural Language MCP server?" → See `/mcp-servers/postgres-nl-mcp/CLAUDE.md`
- "How does SQL injection prevention work in postgres-nl-mcp?" → See `/mcp-servers/postgres-nl-mcp/CLAUDE.md`

Claude has full context from this document and can help with repository-wide development, architecture decisions, and will reference project-specific CLAUDE.md files for detailed project-specific questions.
