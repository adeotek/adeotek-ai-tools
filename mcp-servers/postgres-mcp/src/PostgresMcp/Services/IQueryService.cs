using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for executing read-only database queries.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Executes a read-only SELECT query against the database.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="sql">SQL query to execute (must be SELECT only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results.</returns>
    Task<QueryResult> ExecuteQueryAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a SQL query is safe (read-only, no modifications).
    /// </summary>
    /// <param name="sql">SQL query to validate.</param>
    /// <returns>True if the query is safe, false otherwise.</returns>
    bool ValidateQuerySafety(string sql);
}
