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
    public int MinPoolSize { get; set; }
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
    public List<string> AllowedSchemas { get; set; } = [];

    /// <summary>
    /// List of blocked schemas.
    /// </summary>
    public List<string> BlockedSchemas { get; set; } = ["pg_catalog", "information_schema"];

    /// <summary>
    /// List of blocked tables (regex patterns).
    /// </summary>
    public List<string> BlockedTables { get; set; } = [];

    /// <summary>
    /// Maximum number of rows to return in a single query.
    /// </summary>
    public int MaxRowsPerQuery { get; set; } = 10000;

    /// <summary>
    /// Maximum query execution time in seconds.
    /// </summary>
    public int MaxQueryExecutionSeconds { get; set; } = 30;
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
    public bool LogResults { get; set; }

    /// <summary>
    /// Log file path (optional).
    /// </summary>
    public string? LogFilePath { get; set; }
}
