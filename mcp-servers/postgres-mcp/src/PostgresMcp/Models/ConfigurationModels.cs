namespace PostgresMcp.Models;

/// <summary>
/// PostgreSQL database connection settings.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Postgres";

    /// <summary>
    /// Default connection string (can be overridden via configuration or environment variables).
    /// </summary>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    /// Maximum number of connection retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to use SSL for connections.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Maximum size of connection pool.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum size of connection pool.
    /// </summary>
    public int MinPoolSize { get; set; } = 0;
}

/// <summary>
/// AI/LLM configuration for query generation.
/// </summary>
public class AiOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Ai";

    /// <summary>
    /// OpenAI API key or Azure OpenAI key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model to use for query generation (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Azure OpenAI endpoint (if using Azure).
    /// </summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>
    /// Azure OpenAI deployment name (if using Azure).
    /// </summary>
    public string? AzureDeploymentName { get; set; }

    /// <summary>
    /// Maximum tokens for AI responses.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Temperature for AI responses (0.0 - 1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Whether AI features are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Security and rate limiting options.
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Security";

    /// <summary>
    /// Whether to enable rate limiting.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute per IP.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// List of allowed schemas (empty means all allowed).
    /// </summary>
    public List<string> AllowedSchemas { get; set; } = new();

    /// <summary>
    /// List of blocked schemas.
    /// </summary>
    public List<string> BlockedSchemas { get; set; } = new() { "pg_catalog", "information_schema" };

    /// <summary>
    /// List of blocked tables (regex patterns).
    /// </summary>
    public List<string> BlockedTables { get; set; } = new();

    /// <summary>
    /// Maximum number of rows to return in a single query.
    /// </summary>
    public int MaxRowsPerQuery { get; set; } = 10000;

    /// <summary>
    /// Maximum query execution time in seconds.
    /// </summary>
    public int MaxQueryExecutionSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to allow data modification queries (UPDATE, DELETE, INSERT).
    /// </summary>
    public bool AllowDataModification { get; set; } = false;

    /// <summary>
    /// Whether to allow schema modification queries (CREATE, ALTER, DROP).
    /// </summary>
    public bool AllowSchemaModification { get; set; } = false;
}

/// <summary>
/// Logging configuration options.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Logging";

    /// <summary>
    /// Whether to log SQL queries.
    /// </summary>
    public bool LogQueries { get; set; } = true;

    /// <summary>
    /// Whether to log query results.
    /// </summary>
    public bool LogResults { get; set; } = false;

    /// <summary>
    /// Whether to log AI prompts and responses.
    /// </summary>
    public bool LogAiInteractions { get; set; } = true;

    /// <summary>
    /// Log file path (optional).
    /// </summary>
    public string? LogFilePath { get; set; }
}
