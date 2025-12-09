using System.Text.Json.Serialization;

namespace AdeotekSqlMcp.Models;

/// <summary>
/// Represents an MCP tool definition
/// </summary>
public sealed record McpTool
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("inputSchema")]
    public required Dictionary<string, object> InputSchema { get; init; }
}

/// <summary>
/// Represents an MCP prompt definition
/// </summary>
public sealed record McpPrompt
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("arguments")]
    public IReadOnlyList<McpPromptArgument>? Arguments { get; init; }
}

/// <summary>
/// Represents a prompt argument
/// </summary>
public sealed record McpPromptArgument
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }
}

/// <summary>
/// Tool execution request
/// </summary>
public sealed record McpToolCallRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; init; }
}

/// <summary>
/// Tool execution response
/// </summary>
public sealed record McpToolCallResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 request
/// </summary>
public sealed record JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 response
/// </summary>
public sealed record JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 error
/// </summary>
public sealed record JsonRpcError
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}
