# AdeoTEK AI tools

A collection of production-ready AI tools, agents, and Model Context Protocol (MCP) servers for building intelligent applications.

## Overview

This repository provides reusable, well-tested AI components that can be integrated into your projects or used independently. Each tool is production-grade with proper documentation, testing, and security measures.

## Repository Structure

```
/
├── mcp-servers/     # MCP server implementations
│   └── postgres-mcp/    # PostgreSQL database operations with AI
├── agents/          # AI agents (coming soon)
├── tools/           # Utility tools (coming soon)
└── CLAUDE.md        # Detailed documentation for AI assistants
```

## Available Components

### MCP Servers

#### PostgreSQL MCP Server

An HTTP-based Model Context Protocol server for PostgreSQL database operations with AI-powered query generation.

**Features**:
- 🔍 **Schema Scanner**: Analyze database structure, relationships, and metadata
- 💬 **Natural Language Queries**: Convert plain English to SQL automatically
- 🤖 **AI-Powered SQL Generation**: Advanced query generation with validation and optimization
- 🔒 **Security First**: Read-only by default, SQL injection prevention, rate limiting
- 🐳 **Docker Ready**: Complete Docker setup with PostgreSQL and pgAdmin
- 📊 **Production Ready**: Comprehensive logging, testing, and error handling

**Quick Start**:
```bash
# Using Docker Compose (recommended)
cd mcp-servers/postgres-mcp
docker-compose up -d

# The server will be available at:
# - API: http://localhost:5000
# - Swagger: http://localhost:5000/swagger
# - pgAdmin: http://localhost:8080
```

**Example API Call**:
```bash
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database_data",
    "arguments": {
      "connectionString": "Host=postgres;Database=testdb;Username=postgres;Password=postgres123",
      "query": "Show me all customers who made orders in the last 30 days"
    }
  }'
```

[📖 Full PostgreSQL MCP Documentation](mcp-servers/postgres-mcp/README.md)

## Technology Stack

- **.NET 9**: Modern, high-performance runtime
- **ASP.NET Core**: Web framework with excellent performance
- **Semantic Kernel**: Microsoft's AI orchestration framework
- **Npgsql**: High-performance PostgreSQL driver
- **Docker**: Containerization and deployment
- **xUnit**: Testing framework
- **Serilog**: Structured logging

## Getting Started

### Prerequisites

- **.NET 9 SDK** (for local development)
- **Docker & Docker Compose** (recommended)
- **PostgreSQL** (if not using Docker)
- **OpenAI API Key** (for AI features)

### Configuration

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/ai-tools.git
   cd ai-tools
   ```

2. **Set up environment variables**:
   ```bash
   export OPENAI_API_KEY="your-api-key-here"
   ```

3. **Choose your deployment method**:

   **Option A: Docker (Recommended)**
   ```bash
   cd mcp-servers/postgres-mcp
   docker-compose up -d
   ```

   **Option B: Local Development**
   ```bash
   cd mcp-servers/postgres-mcp/src/PostgresMcp
   dotnet restore
   dotnet run
   ```

## Documentation

- **[CLAUDE.md](CLAUDE.md)**: Comprehensive guide for AI assistants and developers
- **[PostgreSQL MCP Server README](mcp-servers/postgres-mcp/README.md)**: Detailed project documentation
- **Swagger UI**: Available at `http://localhost:5000/swagger` when running

## Security

All components implement security best practices:

- ✅ No hardcoded secrets
- ✅ SQL injection prevention via parameterized queries
- ✅ Rate limiting and request throttling
- ✅ Read-only operations by default
- ✅ Query timeout enforcement
- ✅ Comprehensive input validation
- ✅ Secure connection string handling

## Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific project tests
cd mcp-servers/postgres-mcp/src/PostgresMcp.Tests
dotnet test
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the established patterns in [CLAUDE.md](CLAUDE.md)
4. Write tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Update documentation as needed
7. Commit your changes (`git commit -m 'feat: add amazing feature'`)
8. Push to the branch (`git push origin feature/amazing-feature`)
9. Open a Pull Request

## Roadmap

### Near Term
- [ ] Additional database support (MySQL, MongoDB, SQL Server)
- [ ] GraphQL endpoint for MCP servers
- [ ] Query caching and performance optimization
- [ ] WebSocket support for real-time updates

### Future
- [ ] AI agents for database optimization
- [ ] Schema migration assistant
- [ ] Data quality analyzer
- [ ] Additional MCP servers (Redis, Elasticsearch, etc.)
- [ ] Standalone AI tools and utilities

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with [.NET 9](https://dotnet.microsoft.com/)
- AI powered by [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- Implements [Model Context Protocol](https://modelcontextprotocol.io)

## Support

- 📚 Check [CLAUDE.md](CLAUDE.md) for detailed documentation
- 🐛 Report issues on [GitHub Issues](https://github.com/yourusername/ai-tools/issues)
- 💬 Join discussions in the repository

---

**Made with ❤️ for the AI community**
