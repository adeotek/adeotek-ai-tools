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
    /// Connection string for the PostgreSQL server.
    /// If provided, this overrides individual connection parameters.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Maximum size of connection pool.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum size of connection pool.
    /// </summary>
    public int MinPoolSize { get; set; }
}
