namespace AdeotekSqlMcp.Utilities;

/// <summary>
/// Base exception for all MCP-related errors
/// </summary>
public class McpException : Exception
{
    public int ErrorCode { get; }

    public McpException(string message, int errorCode = -32000) : base(message)
    {
        ErrorCode = errorCode;
    }

    public McpException(string message, Exception innerException, int errorCode = -32000)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when database connection fails
/// </summary>
public sealed class DatabaseConnectionException : McpException
{
    public DatabaseConnectionException(string message)
        : base(message, -32001) { }

    public DatabaseConnectionException(string message, Exception innerException)
        : base(message, innerException, -32001) { }
}

/// <summary>
/// Exception thrown when query validation fails
/// </summary>
public sealed class QueryValidationException : McpException
{
    public QueryValidationException(string message)
        : base(message, -32002) { }

    public QueryValidationException(string message, Exception innerException)
        : base(message, innerException, -32002) { }
}

/// <summary>
/// Exception thrown when query execution fails
/// </summary>
public sealed class QueryExecutionException : McpException
{
    public QueryExecutionException(string message)
        : base(message, -32003) { }

    public QueryExecutionException(string message, Exception innerException)
        : base(message, innerException, -32003) { }
}

/// <summary>
/// Exception thrown when configuration is invalid
/// </summary>
public sealed class ConfigurationException : McpException
{
    public ConfigurationException(string message)
        : base(message, -32004) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException, -32004) { }
}

/// <summary>
/// Exception thrown when operation times out
/// </summary>
public sealed class TimeoutException : McpException
{
    public TimeoutException(string message)
        : base(message, -32005) { }

    public TimeoutException(string message, Exception innerException)
        : base(message, innerException, -32005) { }
}

/// <summary>
/// Exception thrown when tool is not found
/// </summary>
public sealed class ToolNotFoundException : McpException
{
    public ToolNotFoundException(string toolName)
        : base($"Tool not found: {toolName}", -32601) { }
}

/// <summary>
/// Exception thrown when prompt is not found
/// </summary>
public sealed class PromptNotFoundException : McpException
{
    public PromptNotFoundException(string promptName)
        : base($"Prompt not found: {promptName}", -32601) { }
}
