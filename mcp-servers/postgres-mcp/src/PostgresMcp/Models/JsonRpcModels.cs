using System.Text.Json.Serialization;

namespace PostgresMcp.Models;

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
    /// Request ID (string, number, or null). Null for notifications.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// Method name (e.g., "initialize", "tools/list", "tools/call").
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Method parameters (object or array).
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }
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
    /// Request ID (must match the request ID).
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// Result (if successful). Mutually exclusive with Error.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    /// <summary>
    /// Error (if failed). Mutually exclusive with Result.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error object.
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// Error code. Standard codes: -32700 (Parse error), -32600 (Invalid Request),
    /// -32601 (Method not found), -32602 (Invalid params), -32603 (Internal error).
    /// Custom codes: -32000 to -32099 (Server error).
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error data (optional).
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

/// <summary>
/// JSON-RPC error codes.
/// </summary>
public static class JsonRpcErrorCodes
{
    /// <summary>
    /// Invalid JSON was received by the server.
    /// </summary>
    public const int ParseError = -32700;

    /// <summary>
    /// The JSON sent is not a valid Request object.
    /// </summary>
    public const int InvalidRequest = -32600;

    /// <summary>
    /// The method does not exist / is not available.
    /// </summary>
    public const int MethodNotFound = -32601;

    /// <summary>
    /// Invalid method parameter(s).
    /// </summary>
    public const int InvalidParams = -32602;

    /// <summary>
    /// Internal JSON-RPC error.
    /// </summary>
    public const int InternalError = -32603;

    /// <summary>
    /// Server is not initialized.
    /// </summary>
    public const int ServerNotInitialized = -32002;

    /// <summary>
    /// Resource not found.
    /// </summary>
    public const int ResourceNotFound = -32001;

    /// <summary>
    /// Tool execution failed.
    /// </summary>
    public const int ToolExecutionError = -32000;
}

/// <summary>
/// Batch JSON-RPC request (array of requests).
/// </summary>
public class JsonRpcBatchRequest
{
    public List<JsonRpcRequest> Requests { get; set; } = [];
}

/// <summary>
/// Batch JSON-RPC response (array of responses).
/// </summary>
public class JsonRpcBatchResponse
{
    public List<JsonRpcResponse> Responses { get; set; } = [];
}
