# AdeoTEK AI Tools

A collection of AI-powered tools, intelligent agents, and Model Context Protocol (MCP) servers designed to enhance productivity and enable AI-driven workflows.

## 🚀 Overview

This repository contains various AI-related projects organized by type:

- **Agents**: Intelligent agents that use Large Language Models (LLMs) to accomplish specific tasks
- **MCP Servers**: Protocol-compliant servers for database operations and other services
- **Tools**: Utility tools and helpers for AI-powered workflows

## 📦 Projects

### Intelligent HTTP Agent

**Location**: [`/agents/http-agent`](./agents/http-agent)
**Language**: Go 1.23+
**Status**: ✅ Ready to use

An AI-powered HTTP request tool with a beautiful web UI. Think of it as an intelligent `curl` alternative that understands natural language and provides human-readable analysis.

**Features**:
- 🌐 Modern web interface
- 🤖 Natural language query support
- 🔒 Built-in security (SSRF protection, SSL verification)
- ⚡ Fast and lightweight
- 🐳 Docker ready

**Quick Start**:
```bash
cd agents/http-agent
export OPENAI_API_KEY=your-key
go run cmd/server/main.go
# Open http://localhost:8080
```

[Read full documentation →](./agents/http-agent/README.md)

---

### PostgreSQL MCP Server

**Location**: [`/mcp-servers/postgres-mcp`](./mcp-servers/postgres-mcp)
**Language**: .NET 9
**Status**: ✅ Ready to use

A production-ready Model Context Protocol (MCP) server for PostgreSQL database operations with AI-powered query generation and natural language understanding.

**Features**:
- 🔍 **Schema Scanner**: Comprehensive database structure analysis
- 💬 **Natural Language Queries**: Convert plain English to SQL
- 🤖 **AI-Powered SQL Generation**: Advanced query generation with validation
- 🔒 **Security First**: SQL injection prevention, rate limiting, read-only by default
- 📊 **Three MCP Tools**: scan_database_structure, query_database_data, advanced_sql_query
- 🐳 **Docker Ready**: Complete setup with PostgreSQL and pgAdmin

**Quick Start**:
```bash
cd mcp-servers/postgres-mcp
docker-compose up -d
# Open http://localhost:5000/scalar/v1
```

[Read full documentation →](./mcp-servers/postgres-mcp/README.md)

---

## 🎯 Quick Start

### Prerequisites

- **For Go agents**: Go 1.23+
- **For .NET projects**: .NET 9+
- **For Docker**: Docker & Docker Compose
- **API Keys**: OpenAI or Anthropic API key

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/adeotek/adeotek-ai-tools.git
   cd adeotek-ai-tools
   ```

2. Navigate to specific project:
   ```bash
   cd agents/http-agent  # or any other project
   ```

3. Follow project-specific README for setup

## 🏗️ Repository Structure

```
adeotek-ai-tools/
├── agents/              # Intelligent AI agents
│   └── http-agent/      # HTTP request agent with AI analysis
├── mcp-servers/         # Model Context Protocol servers
│   └── postgres-mcp/    # PostgreSQL MCP server with AI query generation
├── tools/               # Additional AI tools (planned)
├── README.md           # This file
├── CLAUDE.md           # Detailed context for Claude AI
└── LICENSE             # MIT License
```

## 🛠️ Technology Stack

- **Go**: Gin web framework, Viper configuration
- **.NET 9**: ASP.NET Core, Semantic Kernel, Npgsql, Scalar (API docs)
- **AI/LLM**: OpenAI, Anthropic Claude, Google Gemini, Ollama, LM Studio
- **Containers**: Docker, Docker Compose
- **Databases**: PostgreSQL
- **Testing**: xUnit, Moq, FluentAssertions
- **Logging**: Serilog (structured logging)

## 📚 Documentation

- **[CLAUDE.md](./CLAUDE.md)**: Comprehensive context for Claude AI interactions
- **Project READMEs**: Each project has detailed documentation in its directory
- **Configuration Examples**: `.env.example` and config files in each project

## 🔧 Configuration

All projects support configuration via:

1. **Environment Variables** (recommended for production)
2. **Configuration Files** (YAML/JSON)
3. **Command-line Arguments** (where applicable)

### Example Environment Variables

```bash
# LLM Configuration
export OPENAI_API_KEY=sk-...
export LLM_PROVIDER=openai
export LLM_MODEL=gpt-4-turbo-preview

# Server Configuration
export PORT=8080
```

## 🐳 Docker Deployment

Each project includes Docker support:

```bash
# Navigate to project directory
cd agents/http-agent

# Copy environment template
cp .env.example .env

# Edit .env with your configuration
nano .env

# Start with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

## 🔒 Security

All projects implement security best practices:

- ✅ No hardcoded secrets
- ✅ Input validation and sanitization
- ✅ SSRF protection for network requests
- ✅ SSL/TLS support
- ✅ Rate limiting
- ✅ Timeout mechanisms
- ✅ Non-root Docker containers

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/new-feature`
3. **Follow code quality standards** (see [CLAUDE.md](./CLAUDE.md))
4. **Add tests** for new functionality
5. **Update documentation**
6. **Submit a pull request**

### Development Guidelines

- Follow language-specific conventions (Go, .NET, etc.)
- Write comprehensive tests
- Document all public APIs
- Use structured logging
- Handle errors properly
- Never commit secrets

See [CLAUDE.md](./CLAUDE.md) for detailed development guidelines.

## 📖 Resources

- **OpenAI Documentation**: https://platform.openai.com/docs
- **Anthropic Documentation**: https://docs.anthropic.com
- **MCP Specification**: https://modelcontextprotocol.io
- **Go**: https://golang.org/doc
- **.NET**: https://dotnet.microsoft.com/learn

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.
