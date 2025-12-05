# PostgreSQL MCP Server - Integration Guide

This guide explains how to integrate the PostgreSQL MCP Server into your applications using the Model Context Protocol (MCP).

## Table of Contents

- [Quick Start](#quick-start)
- [Registering New Tools](#registering-new-tools)
- [Registering New Resources](#registering-new-resources)
- [Sending SSE Notifications](#sending-sse-notifications)
- [MCP Client Integration](#mcp-client-integration)
- [VS Code Integration](#vs-code-integration)
- [Error Handling](#error-handling)

## Quick Start

### 1. Start the Server

**Using Docker:**
```bash
cd mcp-servers/postgres-mcp
docker-compose up -d
```

**Using .NET CLI:**
```bash
cd mcp-servers/postgres-mcp/src/PostgresMcp
dotnet run
```

### 2. Initialize the Server (Legacy Method)

```bash
curl -X POST http://localhost:5000/mcp/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "host": "localhost",
    "port": 5432,
    "username": "postgres",
    "password": "password"
  }'
```

### 3. Test the Connection

```bash
# Ping the server
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "ping",
    "params": {}
  }'

# List available tools
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

## Registering New Tools

### Step 1: Define the Tool Schema

In `McpProtocolEndpoints.cs`, add your tool to the `HandleToolsList()` method:

```csharp
private static object HandleToolsList()
{
    var tools = new List<Tool>
    {
        // Existing tools...

        // Add your new tool
        new()
        {
            Name = "my_custom_tool",
            Description = "Description of what this tool does",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    param1 = new
                    {
                        type = "string",
                        description = "First parameter description"
                    },
                    param2 = new
                    {
                        type = "number",
                        description = "Second parameter description"
                    }
                },
                required = new[] { "param1" }
            }
        }
    };

    return new ListToolsResult { Tools = tools };
}
```

### Step 2: Implement the Tool Handler

Add a case to the `HandleToolsCallAsync()` method:

```csharp
private static async Task<object> HandleToolsCallAsync(...)
{
    var callParams = DeserializeParams<CallToolParams>(request.Params);

    return callParams.Name switch
    {
        "scan_database_structure" => await ExecuteScanDatabaseAsync(...),
        "query_database" => await ExecuteQueryDatabaseAsync(...),

        // Add your custom tool
        "my_custom_tool" => await ExecuteMyCustomToolAsync(callParams.Arguments, ...),

        _ => new CallToolResult
        {
            IsError = true,
            Content = [new Content { Type = "text", Text = $"Unknown tool: {callParams.Name}" }]
        }
    };
}
```

### Step 3: Implement the Tool Logic

```csharp
private static async Task<CallToolResult> ExecuteMyCustomToolAsync(
    Dictionary<string, object?>? arguments,
    // Add other dependencies as needed
    CancellationToken cancellationToken)
{
    try
    {
        // Extract parameters
        var param1 = GetRequiredArgument<string>(arguments, "param1");
        var param2 = GetOptionalArgument<int>(arguments, "param2", 10); // default value

        // Perform your tool's logic
        var result = await DoSomethingAsync(param1, param2, cancellationToken);

        // Return success result
        return new CallToolResult
        {
            IsError = false,
            Content = [new Content
            {
                Type = "text",
                Text = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            }]
        };
    }
    catch (Exception ex)
    {
        // Return error result
        return new CallToolResult
        {
            IsError = true,
            Content = [new Content
            {
                Type = "text",
                Text = $"Error executing my_custom_tool: {ex.Message}"
            }]
        };
    }
}

// Helper for optional arguments
private static T GetOptionalArgument<T>(
    Dictionary<string, object?>? arguments,
    string key,
    T defaultValue)
{
    if (arguments == null || !arguments.TryGetValue(key, out var value) || value == null)
    {
        return defaultValue;
    }

    return GetRequiredArgument<T>(arguments, key);
}
```

### Step 4: Test Your Tool

```bash
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "my_custom_tool",
      "arguments": {
        "param1": "test value",
        "param2": 42
      }
    }
  }'
```

## Registering New Resources

### Step 1: Update ResourceProvider

In `Services/ResourceProvider.cs`, modify the `ListResourcesAsync()` method:

```csharp
public Task<ListResourcesResult> ListResourcesAsync(string? cursor = null)
{
    // Existing resources...

    var resources = new List<Resource>
    {
        // Existing resources...

        // Add your custom resource
        new()
        {
            Uri = $"postgres://{config.Host}:{config.Port}/my_resource",
            Name = "My Custom Resource",
            Description = "Description of the resource",
            MimeType = "application/json"
        }
    };

    // Pagination logic...
}
```

### Step 2: Implement Resource Reader

Update the `ReadResourceAsync()` method:

```csharp
public async Task<ReadResourceResult> ReadResourceAsync(string uri)
{
    // Parse URI
    var uriParts = uri.Replace("postgres://", "").Split('/');
    var resourceType = uriParts.Length >= 2 ? uriParts[1] : string.Empty;

    return resourceType switch
    {
        "connection" => await GetConnectionResourceAsync(uri),
        "databases" => await GetDatabasesResourceAsync(uri),

        // Add your custom resource
        "my_resource" => await GetMyResourceAsync(uri),

        _ => throw new ArgumentException($"Unknown resource type: {uri}")
    };
}

private async Task<ReadResourceResult> GetMyResourceAsync(string uri)
{
    // Fetch your resource data
    var resourceData = await FetchMyResourceDataAsync();

    return new ReadResourceResult
    {
        Contents =
        [
            new ResourceContents
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(resourceData, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            }
        ]
    };
}
```

### Step 3: Test Your Resource

```bash
# List resources
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "resources/list",
    "params": {}
  }'

# Read your resource
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "resources/read",
    "params": {
      "uri": "postgres://localhost:5432/my_resource"
    }
  }'
```

## Sending SSE Notifications

### From Your Code

Inject `ISseNotificationService` and use it to send notifications:

```csharp
public class MyService
{
    private readonly ISseNotificationService _sseService;

    public MyService(ISseNotificationService sseService)
    {
        _sseService = sseService;
    }

    public async Task DoSomethingAsync()
    {
        // Do work...

        // Notify all connected clients
        await _sseService.BroadcastNotificationAsync(
            NotificationTypes.ResourcesUpdated,
            new ResourceUpdatedNotification
            {
                Uri = "postgres://localhost:5432/my_resource"
            });
    }

    public async Task DoLongRunningTaskAsync(string progressToken)
    {
        for (int i = 0; i <= 100; i += 10)
        {
            // Update progress
            await _sseService.BroadcastNotificationAsync(
                NotificationTypes.Progress,
                new ProgressNotification
                {
                    ProgressToken = progressToken,
                    Progress = i / 100.0,
                    Total = 100
                });

            await Task.Delay(1000);
        }
    }
}
```

### Listen to SSE Events

```bash
# Connect and listen
curl -N http://localhost:5000/mcp/v1/sse?clientId=my-client
```

**JavaScript/TypeScript Example:**
```typescript
const eventSource = new EventSource('http://localhost:5000/mcp/v1/sse?clientId=my-client');

eventSource.addEventListener('connected', (event) => {
  console.log('Connected:', JSON.parse(event.data));
});

eventSource.addEventListener('notifications/resources/updated', (event) => {
  const notification = JSON.parse(event.data);
  console.log('Resource updated:', notification.uri);
});

eventSource.addEventListener('notifications/progress', (event) => {
  const progress = JSON.parse(event.data);
  console.log(`Progress: ${progress.progress * 100}%`);
});

eventSource.onerror = (error) => {
  console.error('SSE error:', error);
};
```

## MCP Client Integration

### Python Client Example

```python
import requests
import json

class PostgresMcpClient:
    def __init__(self, base_url="http://localhost:5000"):
        self.base_url = base_url
        self.endpoint = f"{base_url}/mcp/v1/messages"
        self.request_id = 0

    def send_request(self, method, params=None):
        self.request_id += 1
        payload = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": method,
            "params": params or {}
        }

        response = requests.post(
            self.endpoint,
            json=payload,
            headers={"Content-Type": "application/json"}
        )

        return response.json()

    def initialize(self):
        return self.send_request("initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {
                "name": "PythonClient",
                "version": "1.0.0"
            }
        })

    def list_tools(self):
        return self.send_request("tools/list")

    def call_tool(self, name, arguments):
        return self.send_request("tools/call", {
            "name": name,
            "arguments": arguments
        })

    def scan_database(self, database):
        return self.call_tool("scan_database_structure", {
            "database": database
        })

    def query_database(self, database, query):
        return self.call_tool("query_database", {
            "database": database,
            "query": query
        })

# Usage
client = PostgresMcpClient()

# Initialize
init_result = client.initialize()
print("Initialized:", init_result)

# List tools
tools = client.list_tools()
print("Available tools:", tools)

# Scan database
schema = client.scan_database("testdb")
print("Database schema:", schema)

# Query database
results = client.query_database("testdb", "SELECT * FROM customers LIMIT 5")
print("Query results:", results)
```

### TypeScript Client Example

```typescript
class PostgresMcpClient {
  private baseUrl: string;
  private endpoint: string;
  private requestId: number = 0;

  constructor(baseUrl: string = 'http://localhost:5000') {
    this.baseUrl = baseUrl;
    this.endpoint = `${baseUrl}/mcp/v1/messages`;
  }

  private async sendRequest(method: string, params?: any): Promise<any> {
    this.requestId++;

    const response = await fetch(this.endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        jsonrpc: '2.0',
        id: this.requestId,
        method,
        params: params || {},
      }),
    });

    const data = await response.json();

    if (data.error) {
      throw new Error(`MCP Error ${data.error.code}: ${data.error.message}`);
    }

    return data.result;
  }

  async initialize(): Promise<any> {
    return this.sendRequest('initialize', {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: {
        name: 'TypeScriptClient',
        version: '1.0.0',
      },
    });
  }

  async listTools(): Promise<any> {
    return this.sendRequest('tools/list');
  }

  async callTool(name: string, args: Record<string, any>): Promise<any> {
    return this.sendRequest('tools/call', {
      name,
      arguments: args,
    });
  }

  async scanDatabase(database: string): Promise<any> {
    return this.callTool('scan_database_structure', { database });
  }

  async queryDatabase(database: string, query: string): Promise<any> {
    return this.callTool('query_database', { database, query });
  }
}

// Usage
const client = new PostgresMcpClient();

async function main() {
  // Initialize
  const initResult = await client.initialize();
  console.log('Initialized:', initResult);

  // List tools
  const tools = await client.listTools();
  console.log('Available tools:', tools);

  // Scan database
  const schema = await client.scanDatabase('testdb');
  console.log('Database schema:', schema);

  // Query database
  const results = await client.queryDatabase(
    'testdb',
    'SELECT * FROM customers LIMIT 5'
  );
  console.log('Query results:', results);
}

main().catch(console.error);
```

## VS Code Integration

### Using the MCP Settings

1. **Copy the MCP settings:**
   ```bash
   cp .vscode/mcp-settings.json ~/.config/Code/User/mcp-settings.json
   ```

2. **Or manually add to VS Code settings:**
   Open VS Code Settings (JSON) and add:
   ```json
   {
     "mcp.servers": {
       "postgres-mcp": {
         "transport": "http",
         "endpoint": "http://localhost:5000/mcp/v1/messages",
         "sse": "http://localhost:5000/mcp/v1/sse"
       }
     }
   }
   ```

3. **Use the MCP Server in VS Code:**
   - Open Command Palette (Cmd/Ctrl+Shift+P)
   - Type "MCP: Connect to Server"
   - Select "postgres-mcp"
   - Start using MCP tools in your AI assistant

## Error Handling

### Handling JSON-RPC Errors

```typescript
async function safeCallTool(client: PostgresMcpClient, name: string, args: any) {
  try {
    const result = await client.callTool(name, args);

    if (result.isError) {
      console.error(`Tool error: ${result.content[0].text}`);
      return null;
    }

    return JSON.parse(result.content[0].text);
  } catch (error) {
    if (error instanceof Error) {
      console.error(`Request error: ${error.message}`);
    }
    return null;
  }
}
```

### Common Error Codes

- `-32700`: Parse error (invalid JSON)
- `-32600`: Invalid request (malformed JSON-RPC)
- `-32601`: Method not found
- `-32602`: Invalid params
- `-32603`: Internal error
- `-32002`: Server not initialized
- `-32001`: Resource not found
- `-32000`: Tool execution error

### Retry Logic

```typescript
async function callToolWithRetry(
  client: PostgresMcpClient,
  name: string,
  args: any,
  maxRetries: number = 3
): Promise<any> {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await client.callTool(name, args);
    } catch (error) {
      if (attempt === maxRetries) {
        throw error;
      }

      // Exponential backoff
      const delay = Math.pow(2, attempt) * 1000;
      console.log(`Retry ${attempt}/${maxRetries} after ${delay}ms`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
}
```

## Best Practices

1. **Always Initialize First:**
   ```typescript
   await client.initialize();
   await client.sendRequest('initialized'); // Notification
   ```

2. **Use Batch Requests for Multiple Operations:**
   ```typescript
   const batchResponse = await fetch(endpoint, {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify([
       { jsonrpc: '2.0', id: 1, method: 'ping', params: {} },
       { jsonrpc: '2.0', id: 2, method: 'tools/list', params: {} },
     ]),
   });
   ```

3. **Handle SSE Reconnections:**
   ```typescript
   function connectSSE(clientId: string) {
     const es = new EventSource(`/mcp/v1/sse?clientId=${clientId}`);

     es.onerror = () => {
       es.close();
       setTimeout(() => connectSSE(clientId), 5000); // Reconnect after 5s
     };

     return es;
   }
   ```

4. **Validate Tool Arguments:**
   ```typescript
   function validateArgs(schema: any, args: any): boolean {
     // Implement JSON Schema validation
     return true; // simplified
   }
   ```

## Next Steps

- See [sample-requests.md](./sample-requests.md) for more examples
- Check the main [README.md](../README.md) for deployment instructions
- Review [CLAUDE.md](../CLAUDE.md) for development guidelines
