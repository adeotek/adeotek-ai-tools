namespace PostgresMcp.Models;

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
