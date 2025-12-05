# PostgreSQL MCP Server - Sample JSON-RPC Requests

This document contains sample JSON-RPC 2.0 requests for testing the PostgreSQL MCP Server.

## Table of Contents

- [MCP Protocol Lifecycle](#mcp-protocol-lifecycle)
- [Tools](#tools)
- [Resources](#resources)
- [Prompts](#prompts)
- [Server-Sent Events (SSE)](#server-sent-events-sse)
- [Batch Requests](#batch-requests)
- [Legacy API](#legacy-api)

## MCP Protocol Lifecycle

### 1. Initialize

Negotiate capabilities and protocol version with the server.

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {
        "experimental": {},
        "sampling": {}
      },
      "clientInfo": {
        "name": "ExampleClient",
        "version": "1.0.0"
      }
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "serverInfo": {
      "name": "PostgreSQL MCP Server",
      "version": "2.0.0"
    },
    "capabilities": {
      "tools": {
        "listChanged": true
      },
      "resources": {
        "subscribe": true,
        "listChanged": true
      },
      "prompts": {
        "listChanged": true
      }
    }
  }
}
```

### 2. Initialized (Notification)

Notify the server that initialization is complete. This is a notification (no `id` field).

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "initialized",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": null,
  "result": {}
}
```

### 3. Ping

Keep-alive and health check.

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "ping",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {}
}
```

## Tools

### 1. List Tools

Get all available MCP tools.

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/list",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "tools": [
      {
        "name": "scan_database_structure",
        "description": "Scan and analyze PostgreSQL database structure...",
        "inputSchema": {
          "type": "object",
          "properties": {
            "database": {
              "type": "string",
              "description": "Database name to scan"
            }
          },
          "required": ["database"]
        }
      },
      {
        "name": "query_database",
        "description": "Execute a read-only SELECT query...",
        "inputSchema": {
          "type": "object",
          "properties": {
            "database": {
              "type": "string"
            },
            "query": {
              "type": "string"
            }
          },
          "required": ["database", "query"]
        }
      }
    ]
  }
}
```

### 2. Call Tool: scan_database_structure

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "scan_database_structure",
      "arguments": {
        "database": "testdb"
      }
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "isError": false,
    "content": [
      {
        "type": "text",
        "text": "{\n  \"serverVersion\": \"16.1\",\n  \"databaseName\": \"testdb\",\n  \"tables\": [...]\n}"
      }
    ]
  }
}
```

### 3. Call Tool: query_database

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
      "name": "query_database",
      "arguments": {
        "database": "testdb",
        "query": "SELECT * FROM customers LIMIT 10"
      }
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "isError": false,
    "content": [
      {
        "type": "text",
        "text": "{\n  \"columns\": [\"id\", \"name\", \"email\"],\n  \"rows\": [{...}],\n  \"rowCount\": 10,\n  \"executionTimeMs\": 45\n}"
      }
    ]
  }
}
```

### 4. Tool Error Response (Invalid Query)

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 6,
    "method": "tools/call",
    "params": {
      "name": "query_database",
      "arguments": {
        "database": "testdb",
        "query": "DELETE FROM customers"
      }
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "isError": true,
    "content": [
      {
        "type": "text",
        "text": "Query validation failed: Only SELECT queries are allowed..."
      }
    ]
  }
}
```

## Resources

### 1. List Resources

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 7,
    "method": "resources/list",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "resources": [
      {
        "uri": "postgres://localhost:5432/connection",
        "name": "PostgreSQL Connection",
        "description": "Connection to PostgreSQL server...",
        "mimeType": "application/json"
      },
      {
        "uri": "postgres://localhost:5432/databases",
        "name": "Available Databases",
        "description": "List of databases on the server",
        "mimeType": "application/json"
      }
    ],
    "nextCursor": null
  }
}
```

### 2. Read Resource

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 8,
    "method": "resources/read",
    "params": {
      "uri": "postgres://localhost:5432/connection"
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "contents": [
      {
        "uri": "postgres://localhost:5432/connection",
        "mimeType": "application/json",
        "text": "{\n  \"host\": \"localhost\",\n  \"port\": 5432,\n  \"status\": \"connected\"\n}"
      }
    ]
  }
}
```

### 3. Subscribe to Resource

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 9,
    "method": "resources/subscribe",
    "params": {
      "uri": "postgres://localhost:5432/connection"
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "result": {}
}
```

### 4. Unsubscribe from Resource

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 10,
    "method": "resources/unsubscribe",
    "params": {
      "uri": "postgres://localhost:5432/connection"
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": {}
}
```

## Prompts

### 1. List Prompts

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 11,
    "method": "prompts/list",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "prompts": [
      {
        "name": "analyze_table",
        "description": "Generate a query to analyze a table",
        "arguments": [
          {
            "name": "database",
            "description": "Database name",
            "required": true
          },
          {
            "name": "table",
            "description": "Table name",
            "required": true
          }
        ]
      },
      {
        "name": "find_relationships",
        "description": "Find relationships between tables",
        "arguments": [...]
      }
    ]
  }
}
```

### 2. Get Prompt

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 12,
    "method": "prompts/get",
    "params": {
      "name": "analyze_table",
      "arguments": {
        "database": "testdb",
        "table": "customers"
      }
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "result": {
    "description": "Analyze table 'customers' in database 'testdb'",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "You have access to a PostgreSQL database 'testdb'...\n\nPlease analyze the table 'customers'..."
        }
      }
    ]
  }
}
```

## Server-Sent Events (SSE)

Connect to the SSE endpoint to receive real-time notifications.

**Connect:**
```bash
curl -N http://localhost:5000/mcp/v1/sse?clientId=client-123
```

**Sample Events:**
```
id: 1
event: connected
data: {"clientId":"client-123","timestamp":"2024-12-03T10:00:00Z"}

:heartbeat

id: 2
event: notifications/resources/updated
data: {"uri":"postgres://localhost:5432/connection"}

id: 3
event: notifications/tools/list_changed
data: {}
```

## Batch Requests

Send multiple JSON-RPC requests in a single HTTP request.

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '[
    {
      "jsonrpc": "2.0",
      "id": 1,
      "method": "ping",
      "params": {}
    },
    {
      "jsonrpc": "2.0",
      "id": 2,
      "method": "tools/list",
      "params": {}
    },
    {
      "jsonrpc": "2.0",
      "id": 3,
      "method": "resources/list",
      "params": {}
    }
  ]'
```

**Response:**
```json
[
  {
    "jsonrpc": "2.0",
    "id": 1,
    "result": {}
  },
  {
    "jsonrpc": "2.0",
    "id": 2,
    "result": {
      "tools": [...]
    }
  },
  {
    "jsonrpc": "2.0",
    "id": 3,
    "result": {
      "resources": [...]
    }
  }
]
```

## Error Responses

### Method Not Found

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 99,
    "method": "unknown/method",
    "params": {}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 99,
  "error": {
    "code": -32601,
    "message": "Unknown method: unknown/method",
    "data": null
  }
}
```

### Invalid Params

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 100,
    "method": "tools/call",
    "params": {
      "name": "scan_database_structure"
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "error": {
    "code": -32602,
    "message": "Required argument 'database' is missing",
    "data": null
  }
}
```

### Parse Error

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{invalid json'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": null,
  "error": {
    "code": -32700,
    "message": "Invalid JSON",
    "data": "..."
  }
}
```

## Legacy API

The server also supports legacy REST-like endpoints for backward compatibility.

### Initialize (Legacy)

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "host": "localhost",
    "port": 5432,
    "username": "postgres",
    "password": "password",
    "useSsl": true
  }'
```

### Get Tools (Legacy)

**Request:**
```bash
curl http://localhost:5000/mcp/tools
```

### Call Tool (Legacy)

**Request:**
```bash
curl -X POST http://localhost:5000/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "query_database",
    "arguments": {
      "database": "testdb",
      "query": "SELECT * FROM customers LIMIT 10"
    }
  }'
```

## Discovery Endpoint

Get server discovery information.

**Request:**
```bash
curl http://localhost:5000/.well-known/mcp.json
```

**Response:**
```json
{
  "mcpServers": {
    "postgres": {
      "endpoint": "/mcp/v1/messages",
      "sse": "/mcp/v1/sse",
      "transport": "http",
      "capabilities": ["tools", "resources", "prompts"]
    }
  }
}
```

## Testing Tips

1. **Use jq for pretty output:**
   ```bash
   curl ... | jq
   ```

2. **Save requests to files:**
   ```bash
   cat > request.json <<EOF
   {
     "jsonrpc": "2.0",
     "id": 1,
     "method": "tools/list",
     "params": {}
   }
   EOF

   curl -X POST http://localhost:5000/mcp/v1/messages \
     -H "Content-Type: application/json" \
     -d @request.json | jq
   ```

3. **Test SSE with timeout:**
   ```bash
   timeout 30 curl -N http://localhost:5000/mcp/v1/sse?clientId=test-client
   ```

4. **Use Postman or Bruno for GUI testing**

5. **Enable debug logging:**
   ```bash
   export ASPNETCORE_ENVIRONMENT=Development
   export Logging__LogLevel__Default=Debug
   ```
