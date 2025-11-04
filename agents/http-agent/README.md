# Intelligent HTTP Agent

An AI-powered HTTP request tool that understands natural language prompts and provides human-readable responses about HTTP requests. Think of it as an intelligent `curl` alternative with a beautiful web UI.

## Features

- 🚀 **Natural Language Interface**: Ask questions about HTTP requests in plain English
- 🌐 **Web UI**: Modern, responsive interface with real-time results
- 🤖 **AI-Powered Analysis**: Uses OpenAI GPT or Anthropic Claude for intelligent response interpretation
- 🔒 **Security First**: Built-in SSRF protection, SSL verification, and private IP blocking
- 🐳 **Docker Ready**: Easy deployment with Docker and docker-compose
- ⚡ **Fast & Lightweight**: Built in Go for optimal performance
- 📊 **Rich Response Display**: Formatted JSON, status codes with colors, timing information

## Quick Start

### Prerequisites

- Go 1.23+ (for local development)
- Docker & Docker Compose (for containerized deployment)
- OpenAI API key or Anthropic API key

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/adeotek/adeotek-ai-tools.git
   cd adeotek-ai-tools/agents/http-agent
   ```

2. **Set up environment variables**
   ```bash
   cp .env.example .env
   # Edit .env and add your API key
   export OPENAI_API_KEY=your-api-key-here
   ```

3. **Install dependencies**
   ```bash
   go mod download
   ```

4. **Run the application**
   ```bash
   go run cmd/server/main.go
   ```

5. **Open your browser**
   ```
   http://localhost:8080
   ```

### Docker Deployment

1. **Create environment file**
   ```bash
   cp .env.example .env
   # Edit .env and configure your settings
   ```

2. **Start with Docker Compose**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   ```
   http://localhost:8080
   ```

4. **View logs**
   ```bash
   docker-compose logs -f
   ```

5. **Stop the service**
   ```bash
   docker-compose down
   ```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `LLM_PROVIDER` | `openai` | LLM provider: `openai` or `anthropic` |
| `LLM_API_KEY` | - | Your API key (required) |
| `LLM_MODEL` | `gpt-4-turbo-preview` | Model to use |
| `PORT` | `8080` | Server port |
| `HTTP_TIMEOUT` | `30` | HTTP request timeout (seconds) |
| `VERIFY_SSL` | `true` | Verify SSL certificates |
| `BLOCK_PRIVATE_IPS` | `true` | Block private IP addresses |

### Configuration File

Alternatively, create `config/config.yaml`:

```yaml
server:
  port: "8080"
  host: "0.0.0.0"

llm:
  provider: "openai"
  api_key: "your-key-here"
  model: "gpt-4-turbo-preview"

http:
  timeout: 30
  verify_ssl: true
  block_private_ips: true
```

## Usage Examples

### Web UI

1. **Simple GET Request**
   - URL: `https://api.github.com/users/github`
   - Method: `GET`
   - Prompt: "Tell me about this GitHub user"

2. **POST with JSON Body**
   - URL: `https://httpbin.org/post`
   - Method: `POST`
   - Headers: `Content-Type: application/json`
   - Body: `{"name": "test", "value": 123}`
   - Prompt: "Did the request succeed? Show me what was echoed back."

3. **API Health Check**
   - URL: `https://api.example.com/health`
   - Method: `GET`
   - Prompt: "Is this API healthy? What's the response time?"

4. **Debugging Slow Endpoints**
   - URL: `https://slow-api.example.com/endpoint`
   - Method: `GET`
   - Prompt: "Is this endpoint slow? What could be causing the delay?"

### Sample Questions

The AI can answer various questions about your HTTP requests:

- "Is the URL accessible?"
- "What is the response code?"
- "What are the response headers?"
- "What is the response content?"
- "How long did the request take?"
- "Is this API working correctly?"
- "What errors occurred?"
- "Is the response cached?"
- "Show me the user data"
- "Summarize the response"

## API Endpoints

### `GET /`
Returns the main web UI.

### `POST /api/request`
Executes an HTTP request and returns AI-powered analysis.

**Request Body:**
```json
{
  "url": "https://api.example.com/endpoint",
  "method": "GET",
  "headers": {
    "Authorization": "Bearer token",
    "Content-Type": "application/json"
  },
  "body": "{\"key\": \"value\"}",
  "prompt": "What is the response code?"
}
```

**Response:**
```json
{
  "request": { /* original request */ },
  "response": {
    "status_code": 200,
    "status": "200 OK",
    "headers": { /* response headers */ },
    "body": "{ /* response body */ }",
    "duration": 234567890,
    "content_type": "application/json",
    "content_length": 1234
  },
  "analysis": "The API returned a successful 200 OK response...",
  "formatted_body": "{ /* pretty-printed JSON */ }",
  "request_duration": "234.57ms",
  "status_color": "success"
}
```

### `GET /health`
Returns health status of the service.

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
└── README.md               # This file
```

### How It Works

1. **User submits request**: Via web UI with URL, method, headers, body, and optional prompt
2. **HTTP execution**: Go HTTP client makes the request with security checks
3. **AI analysis**: Request/response data is sent to LLM with system prompt
4. **Response formatting**: JSON is pretty-printed, status codes are color-coded
5. **Display results**: Web UI shows status, timing, headers, body, and AI analysis

## Security

- ✅ **SSRF Protection**: Blocks requests to private IP ranges by default
- ✅ **SSL Verification**: Validates SSL certificates (configurable)
- ✅ **Response Size Limits**: Prevents memory exhaustion (10MB default)
- ✅ **Request Timeouts**: Prevents hanging requests (30s default)
- ✅ **Input Validation**: Validates and sanitizes all inputs
- ✅ **No Secrets in Logs**: API keys are never logged

## Development

### Building from Source

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

### Running Tests

```bash
go test ./...
```

### Code Formatting

```bash
go fmt ./...
```

## Troubleshooting

### "LLM API key is required" Error

Make sure you've set one of these environment variables:
```bash
export OPENAI_API_KEY=your-key
# OR
export ANTHROPIC_API_KEY=your-key
```

### "Access to private IP addresses is blocked" Error

This is a security feature. To allow requests to private IPs (e.g., localhost), set:
```bash
export HTTP_AGENT_HTTP_BLOCK_PRIVATE_IPS=false
```

### Request Timeout

Increase the timeout value:
```bash
export HTTP_AGENT_HTTP_TIMEOUT=60  # 60 seconds
```

### Docker Build Issues

Clear Docker cache and rebuild:
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up
```

## Future Enhancements

- [ ] Request history and favorites
- [ ] Export results to various formats
- [ ] WebSocket support
- [ ] GraphQL query support
- [ ] Request collection/workspace management
- [ ] Response diffing
- [ ] Automated testing sequences
- [ ] Performance benchmarking
- [ ] Authentication for web UI

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.

## Support

For issues and questions:
- GitHub Issues: [adeotek/adeotek-ai-tools](https://github.com/adeotek/adeotek-ai-tools/issues)
- Documentation: [CLAUDE.md](../../CLAUDE.md)

## Acknowledgments

- Built with [Gin](https://github.com/gin-gonic/gin) web framework
- Configuration management by [Viper](https://github.com/spf13/viper)
- AI integration with OpenAI and Anthropic APIs
