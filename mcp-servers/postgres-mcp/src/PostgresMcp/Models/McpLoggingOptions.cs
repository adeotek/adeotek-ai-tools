namespace PostgresMcp.Models;

/// <summary>
/// Logging configuration options.
/// </summary>
public class McpLoggingOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "McpLogging";

    /// <summary>
    /// Whether to log SQL queries.
    /// </summary>
    public bool LogQueries { get; set; } = true;

    /// <summary>
    /// Whether to log query results.
    /// </summary>
    public bool LogResults { get; set; }
}
