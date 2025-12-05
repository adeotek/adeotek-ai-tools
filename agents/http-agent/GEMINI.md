# HTTP Agent - Context for Gemini

This document provides comprehensive context about the Intelligent HTTP Agent for Gemini (both CLI and Web) interactions.

## Project Overview

**Purpose**: An AI-powered HTTP request tool that acts as an intelligent `curl` alternative. It makes HTTP/HTTPS requests and provides natural language analysis of the results.

**Technology Stack**: Go 1.24.7+, Gin web framework, Multiple LLM providers

**Location in Repository**: `/agents/http-agent`

## Architecture

### Project Structure

```
http-agent/
├── cmd/
│   └── server/
│       └── main.go          # Application entry point
├── internal/
│   ├── agent/
│   │   ├── agent.go         # Main agent logic
│   │   ├── http_client.go   # HTTP client implementation
│   │   └── llm.go           # LLM integration
│   ├── handlers/
│   │   ├── web.go           # HTTP handlers
│   │   ├── templates/       # HTML templates
│   │   └── static/          # Static assets
│   └── models/
│       └── request.go       # Data models
├── config/
│   └── config.example.yaml  # Configuration example
├── Dockerfile               # Docker build file
├── docker-compose.yml       # Docker Compose config
├── README.md                # User-facing documentation
└── GEMINI.md                # This file - Gemini context
```

### Design Pattern

The HTTP Agent follows a clean architecture pattern:

1. **Models Layer** (`internal/models/`): Data structures for requests, responses, and configuration
2. **Agent Layer** (`internal/agent/`): Core business logic including HTTP client and LLM integration
3. **Handler Layer** (`internal/handlers/`): HTTP handlers, web UI, and API endpoints
4. **Main Entry Point** (`cmd/server/`): Application initialization and server setup

### Key Components

**HTTP Client** (`internal/agent/http_client.go`):
- Makes HTTP/HTTPS requests with security checks
- Performs DNS diagnostics (hostname resolution, IP lookup, timing)
- SSL certificate inspection (validation, expiration, CA details)
- Configurable SSL verification for self-signed certificates
- SSRF protection and private IP blocking
- Request timeouts and response size limits

**LLM Integration** (`internal/agent/llm.go`):
- Abstract interface for multiple LLM providers
- Support for cloud providers: OpenAI, Anthropic Claude, Google Gemini
- Support for local providers: Ollama, LM Studio
- Structured prompts with system and user messages
- Error handling and fallback mechanisms

**Web Handler** (`internal/handlers/web.go`):
- Serves the web UI (HTML/CSS/JavaScript)
- API endpoint for executing requests
- Response formatting (JSON pretty-printing, status code coloring)
- Health check endpoint

## Key Features

### Core Capabilities

1. **Natural Language Interface**: Ask questions about HTTP requests in plain English
2. **Multiple HTTP Methods**: Support for GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS
3. **AI-Powered Analysis**: Context-aware response interpretation
4. **DNS Diagnostics**: Automatic hostname resolution with IP lookup and timing
5. **SSL Certificate Inspection**: Certificate validation, expiration, CA details
6. **Configurable SSL Verification**: Per-request SSL verification toggle
7. **Security**: SSRF protection, private IP blocking, request timeouts

### LLM Provider Support

**Cloud Providers**:
- **OpenAI**: GPT-4, GPT-4o, GPT-3.5-turbo
- **Anthropic**: Claude 3.5 Sonnet, Claude 3 Opus, Claude 3 Sonnet
- **Google Gemini**: Gemini 1.5 Pro, Gemini 1.5 Flash

**Local Providers**:
- **Ollama**: Llama 2, Llama 3, Mistral, CodeLlama, Phi, Gemma
- **LM Studio**: Any locally loaded model via OpenAI-compatible API

### Security Features

- ✅ **SSRF Protection**: Blocks requests to private IP ranges by default
- ✅ **SSL Verification**: Validates SSL certificates (configurable per request)
- ✅ **Response Size Limits**: Prevents memory exhaustion (10MB default)
- ✅ **Request Timeouts**: Prevents hanging requests (30s default)
- ✅ **Input Validation**: Validates and sanitizes all inputs
- ✅ **No Secrets in Logs**: API keys are never logged

## Configuration

### Configuration Methods

The HTTP Agent supports multiple configuration methods (in priority order):
1. **Environment variables** (highest priority)
2. **Configuration file** (`config/config.yaml`)
3. **Default values** (lowest priority)

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `LLM_PROVIDER` | `openai` | LLM provider: `openai`, `anthropic`, `gemini`, `ollama`, or `lmstudio` |
| `LLM_API_KEY` | - | Your API key (required for cloud providers) |
| `LLM_MODEL` | `gpt-4-turbo-preview` | Model to use |
| `HTTP_AGENT_LLM_BASE_URL` | - | Base URL for Ollama/LM Studio |
| `PORT` | `8080` | Server port |
| `HTTP_TIMEOUT` | `30` | HTTP request timeout (seconds) |
| `VERIFY_SSL` | `true` | Verify SSL certificates globally |
| `BLOCK_PRIVATE_IPS` | `true` | Block private IP addresses |
| `GIN_MODE` | `release` | Gin mode: `release`, `debug`, `test` |
| `LOG_LEVEL` | `info` | Log level: `debug`, `info`, `warn`, `error` |

### LLM Provider Configuration Examples

#### OpenAI
```bash
export LLM_PROVIDER=openai
export OPENAI_API_KEY=sk-...
export LLM_MODEL=gpt-4-turbo-preview
```

#### Anthropic Claude
```bash
export LLM_PROVIDER=anthropic
export ANTHROPIC_API_KEY=sk-ant-...
export LLM_MODEL=claude-3-5-sonnet-20241022
```

#### Google Gemini
```bash
export LLM_PROVIDER=gemini
export GEMINI_API_KEY=AIza...
export LLM_MODEL=gemini-1.5-pro
```

#### Ollama (Local)
```bash
export LLM_PROVIDER=ollama
export LLM_MODEL=llama2
export HTTP_AGENT_LLM_BASE_URL=http://localhost:11434
# Ensure Ollama is running: ollama serve
# Pull model first: ollama pull llama2
```

#### LM Studio (Local)
```bash
export LLM_PROVIDER=lmstudio
export LLM_MODEL=local-model
export HTTP_AGENT_LLM_BASE_URL=http://localhost:1234
# Ensure LM Studio server is running
```

### Configuration File

Create `config/config.yaml`:

```yaml
server:
  port: "8080"
  host: "0.0.0.0"

llm:
  provider: "openai"  # openai, anthropic, gemini, ollama, lmstudio
  api_key: "your-key-here"
  model: "gpt-4-turbo-preview"
  base_url: ""  # only for ollama/lmstudio
  max_tokens: 2000
  temperature: 0.7

http:
  timeout: 30
  verify_ssl: true
  block_private_ips: true
  max_response_size_mb: 10
```

## Development Workflow

### Local Development

1. **Set up environment**:
   ```bash
   cd agents/http-agent
   cp .env.example .env
   # Edit .env with your settings
   export $(cat .env | xargs)
   ```

2. **Install dependencies**:
   ```bash
   go mod download
   ```

3. **Run the application**:
   ```bash
   go run cmd/server/main.go
   ```

4. **Access the web UI**:
   ```
   http://localhost:8080
   ```

### Building

```bash
# Build for current platform
go build -o http-agent ./cmd/server/main.go

# Build for Linux
GOOS=linux GOARCH=amd64 go build -o http-agent-linux ./cmd/server/main.go

# Build for macOS
GOOS=darwin GOARCH=amd64 go build -o http-agent-mac ./cmd/server/main.go

# Build for Windows
GOOS=windows GOARCH=amd64 go build -o http-agent.exe ./cmd/server/main.go
```

### Testing

```bash
# Run all tests
go test ./...

# Run tests with coverage
go test -cover ./...

# Run tests with verbose output
go test -v ./...

# Run tests for specific package
go test ./internal/agent/...
```

### Code Quality

```bash
# Format code
go fmt ./...

# Lint code (requires golangci-lint)
golangci-lint run

# Check for common mistakes
go vet ./...

# Update dependencies
go mod tidy
```

## Docker Deployment

### Using Docker Compose (Recommended)

1. **Create environment file**:
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

2. **Start the service**:
   ```bash
   docker-compose up -d
   ```

3. **View logs**:
   ```bash
   docker-compose logs -f
   ```

4. **Stop the service**:
   ```bash
   docker-compose down
   ```

### Using Docker Directly

```bash
# Build the image
docker build -t http-agent:latest .

# Run the container
docker run -d \
  -p 8080:8080 \
  -e OPENAI_API_KEY=sk-... \
  -e LLM_PROVIDER=openai \
  -e LLM_MODEL=gpt-4-turbo-preview \
  --name http-agent \
  http-agent:latest

# View logs
docker logs -f http-agent

# Stop and remove
docker stop http-agent
docker rm http-agent
```

## API Endpoints

### `GET /`
Returns the main web UI (HTML page).

### `POST /api/request`
Executes an HTTP request and returns AI-powered analysis.

**Request Body**:
```json
{
  "url": "https://api.example.com/endpoint",
  "method": "GET",
  "headers": {
    "Authorization": "Bearer token",
    "Content-Type": "application/json"
  },
  "body": "{\"key\": \"value\"}",
  "prompt": "What is the response code?",
  "verify_ssl": true
}
```

**Response**:
```json
{
  "request": {
    "url": "https://api.example.com/endpoint",
    "method": "GET",
    "headers": {...}
  },
  "dns_info": {
    "hostname": "api.example.com",
    "ip_addresses": ["1.2.3.4"],
    "lookup_time_ms": 45.23
  },
  "ssl_info": {
    "subject": "api.example.com",
    "issuer": "Let's Encrypt Authority X3",
    "valid_from": "2024-01-01T00:00:00Z",
    "valid_until": "2024-12-31T23:59:59Z",
    "days_until_expiration": 180
  },
  "response": {
    "status_code": 200,
    "status": "200 OK",
    "headers": {...},
    "body": "{...}",
    "duration": 234567890,
    "content_type": "application/json",
    "content_length": 1234
  },
  "analysis": "The API returned a successful 200 OK response...",
  "formatted_body": "{\n  \"key\": \"value\"\n}",
  "request_duration": "234.57ms",
  "status_color": "success"
}
```

### `GET /health`
Returns health status of the service.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-12-02T10:30:00Z"
}
```

## Common Use Cases

### API Testing
```
URL: https://api.github.com/users/github
Method: GET
Prompt: "Is this API accessible? What's the response structure?"
```

### Debugging Endpoints
```
URL: https://your-api.com/slow-endpoint
Method: GET
Prompt: "Why is this endpoint slow? What's the response time?"
```

### SSL Certificate Inspection
```
URL: https://example.com
Method: GET
Prompt: "Is the SSL certificate valid? When does it expire?"
```

### Testing with Self-Signed Certificates
```
URL: https://localhost:8443/api
Method: GET
Verify SSL: false (unchecked)
Prompt: "Can I connect to this local API?"
```

### POST Request Testing
```
URL: https://httpbin.org/post
Method: POST
Headers: Content-Type: application/json
Body: {"name": "test", "value": 123}
Prompt: "Did the POST succeed? Show me the echoed data."
```

## Troubleshooting

### Common Issues

**Issue**: "LLM API key is required" error
```
Error: "LLM API key is required for provider: openai"
```
**Solution**:
```bash
export OPENAI_API_KEY=sk-...
# Or for Anthropic:
export ANTHROPIC_API_KEY=sk-ant-...
```

**Issue**: "Access to private IP addresses is blocked"
```
Error: "Access to private IP addresses is blocked"
```
**Solution**:
```bash
# For local testing only
export BLOCK_PRIVATE_IPS=false
```

**Issue**: Request timeout
```
Error: "context deadline exceeded"
```
**Solution**:
```bash
# Increase timeout (in seconds)
export HTTP_TIMEOUT=60
```

**Issue**: Port already in use
```
Error: "bind: address already in use"
```
**Solution**:
```bash
# Change the port
export PORT=8081
```

**Issue**: Docker build fails
```
Error: "failed to solve with frontend dockerfile.v0"
```
**Solution**:
```bash
docker-compose build --no-cache
```

**Issue**: SSL verification fails with self-signed certificate
```
Error: "x509: certificate signed by unknown authority"
```
**Solution**: Uncheck the "Verify SSL" checkbox in the web UI for that specific request, or:
```bash
# Disable globally (not recommended for production)
export VERIFY_SSL=false
```

### Debug Mode

Enable debug logging:

```bash
# Set Gin to debug mode
export GIN_MODE=debug

# Set log level to debug
export LOG_LEVEL=debug

# Run the application
go run cmd/server/main.go
```

### Testing Locally

```bash
# Test the health endpoint
curl http://localhost:8080/health

# Test an API request
curl -X POST http://localhost:8080/api/request \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://api.github.com/users/github",
    "method": "GET",
    "prompt": "Tell me about this user"
  }'
```

## Working with Gemini

### When Adding Features

1. **Read the code first**: Use the Read tool to understand existing implementation
2. **Follow Go conventions**: Use standard Go project layout and idioms
3. **Update tests**: Add unit tests for new functionality
4. **Update documentation**: Update both README.md and this GEMINI.md
5. **Test thoroughly**: Build, run, and test your changes locally

### When Debugging

1. **Check logs**: Look for error messages in the console output
2. **Verify configuration**: Ensure all required environment variables are set
3. **Test endpoints**: Use curl or the web UI to test functionality
4. **Review code**: Check for common Go issues (nil pointers, missing error handling)
5. **Enable debug mode**: Set `GIN_MODE=debug` and `LOG_LEVEL=debug`

### When Refactoring

1. **Maintain compatibility**: Don't break the existing API
2. **Update tests**: Ensure all tests pass after changes
3. **Follow patterns**: Maintain consistency with existing code style
4. **Document changes**: Update comments and documentation

## Code Quality Standards

### Go Best Practices

- Use Go 1.23+ features where appropriate
- Follow standard Go project layout
- Implement proper error handling (never ignore errors)
- Use context for cancellation and timeout
- Document exported functions and types
- Use interfaces for abstraction
- Write idiomatic Go code

### Code Organization

- Keep packages focused and cohesive
- Use dependency injection
- Separate concerns (models, agent logic, handlers)
- Avoid circular dependencies
- Use internal/ for non-exported packages

### Testing

- Write unit tests for business logic
- Use table-driven tests where appropriate
- Mock external dependencies (LLM APIs, HTTP clients)
- Test error cases and edge conditions
- Aim for reasonable test coverage (>70%)

### Documentation

- Add GoDoc comments for exported functions/types
- Include usage examples in comments
- Document configuration options
- Keep README.md and GEMINI.md up to date
- Add inline comments for complex logic

## Performance Considerations

- Use connection pooling for HTTP clients
- Implement request timeouts
- Limit response body size
- Use streaming for large responses (future)
- Cache LLM responses where appropriate (future)
- Profile and optimize hot paths

## Security Considerations

- Never hardcode API keys or secrets
- Validate and sanitize all inputs
- Implement SSRF protection
- Use SSL/TLS for all external connections
- Support configurable SSL verification
- Implement rate limiting (future)
- Log security events
- Never log sensitive information

## Future Enhancements

- [ ] Request history and favorites
- [ ] Export results to various formats (JSON, Markdown, HTML)
- [ ] WebSocket support for real-time updates
- [ ] GraphQL query support
- [ ] Request collection/workspace management
- [ ] Response diffing
- [ ] Automated testing sequences
- [ ] Performance benchmarking
- [ ] Authentication for web UI
- [ ] Rate limiting
- [ ] Response caching

## Related Documentation

- **Main Repository Context**: `/GEMINI.md` - Repository-wide guidelines and patterns
- **User Documentation**: `README.md` - User-facing documentation
- **Configuration Example**: `config/config.example.yaml` - Full configuration reference

## Questions for Gemini

When working on the HTTP Agent, you can ask:

- "How does the HTTP client handle SSL certificates?"
- "What LLM providers are supported and how do I add a new one?"
- "How does the SSRF protection work?"
- "Where should I add a new configuration option?"
- "How do I test the agent with a local LLM?"
- "What's the flow from web UI to AI analysis?"
- "How are errors handled in the HTTP client?"
- "Where should I add a new API endpoint?"

Gemini has full context from this document and can help with development, debugging, and architecture decisions for the HTTP Agent project.
