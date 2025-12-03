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
    /// Maximum size of connection pool.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum size of connection pool.
    /// </summary>
    public int MinPoolSize { get; set; }
}

/// <summary>
/// PostgreSQL server connection parameters.
/// These parameters are configured at MCP initialization and used to build connection strings.
/// </summary>
public class ServerConnectionOptions
{
    /// <summary>
    /// PostgreSQL server host/address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// PostgreSQL server port.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// PostgreSQL username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// PostgreSQL password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Whether the configuration has been initialized.
    /// </summary>
    public bool IsConfigured { get; set; }
}
