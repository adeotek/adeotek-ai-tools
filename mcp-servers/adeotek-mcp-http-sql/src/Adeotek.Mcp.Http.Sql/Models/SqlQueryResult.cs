namespace Adeotek.Mcp.Http.Sql.Models;

/// <summary>
/// Represents the result of a database query.
/// </summary>
public class SqlQueryResult
{
    /// <summary>
    /// List of column names.
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// Query result rows (each row is a dictionary of column name to value).
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    public int RowCount => Rows.Count;

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether the results were truncated due to max rows limit.
    /// </summary>
    public bool IsTruncated { get; set; }
}
