namespace PostgresNaturalLanguageMcp.Models;

/// <summary>
/// Represents an MCP tool definition following the Model Context Protocol specification.
/// </summary>
public record McpTool
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// JSON Schema defining the input parameters for the tool.
    /// </summary>
    public required object InputSchema { get; init; }
}

/// <summary>
/// Request to invoke an MCP tool.
/// </summary>
public record McpToolCallRequest
{
    /// <summary>
    /// The name of the tool to invoke.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Arguments to pass to the tool as a JSON object.
    /// </summary>
    public required Dictionary<string, object?> Arguments { get; init; }
}

/// <summary>
/// Response from an MCP tool invocation.
/// </summary>
public record McpToolCallResponse
{
    /// <summary>
    /// Whether the tool execution was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The result data from the tool execution.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Error message if the execution failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Additional metadata about the execution.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// List tools response following MCP specification.
/// </summary>
public record McpListToolsResponse
{
    /// <summary>
    /// Available tools.
    /// </summary>
    public required List<McpTool> Tools { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 request wrapper.
/// </summary>
public record JsonRpcRequest
{
    /// <summary>
    /// JSON-RPC version (must be "2.0").
    /// </summary>
    public string Jsonrpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Method name to invoke.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Method parameters.
    /// </summary>
    public object? Params { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 response wrapper.
/// </summary>
public record JsonRpcResponse
{
    /// <summary>
    /// JSON-RPC version (must be "2.0").
    /// </summary>
    public string Jsonrpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Result object (present if successful).
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Error object (present if failed).
    /// </summary>
    public JsonRpcError? Error { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 error object.
/// </summary>
public record JsonRpcError
{
    /// <summary>
    /// Error code.
    /// </summary>
    public required int Code { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Additional error data.
    /// </summary>
    public object? Data { get; init; }
}
