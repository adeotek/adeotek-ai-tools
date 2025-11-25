using System.Text.Json.Serialization;

namespace PostgresMcp.Models;

/// <summary>
/// MCP Tool definition.
/// </summary>
public class McpTool
{
    /// <summary>
    /// Tool name (e.g., "scan_database_structure").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Input schema (JSON Schema format).
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

/// <summary>
/// MCP Tool list response.
/// </summary>
public class McpToolsResponse
{
    /// <summary>
    /// List of available tools.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = [];
}

/// <summary>
/// MCP Tool call request.
/// </summary>
public class McpToolCallRequest
{
    /// <summary>
    /// Name of the tool to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool arguments (varies by tool).
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object?> Arguments { get; set; } = [];
}

/// <summary>
/// MCP Tool call response.
/// </summary>
public class McpToolCallResponse
{
    /// <summary>
    /// Whether the tool call was successful.
    /// </summary>
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }

    /// <summary>
    /// Content of the response.
    /// </summary>
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = [];
}

/// <summary>
/// MCP Content item.
/// </summary>
public class McpContent
{
    /// <summary>
    /// Content type (usually "text").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// JSON-RPC 2.0 request.
/// </summary>
public class JsonRpcRequest
{
    /// <summary>
    /// JSON-RPC version (always "2.0").
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// Method name.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Method parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object?>? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 response.
/// </summary>
public class JsonRpcResponse
{
    /// <summary>
    /// JSON-RPC version (always "2.0").
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Request ID (same as request).
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// Result (if successful).
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Error (if failed).
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error.
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// Error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
