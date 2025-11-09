using PostgresNaturalLanguageMcp.Models;

namespace PostgresNaturalLanguageMcp.Services;

/// <summary>
/// Service for executing database queries with relationship awareness.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Queries database data based on a natural language request.
    /// Automatically detects and follows foreign key relationships.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="naturalLanguageQuery">Natural language description of what data to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results with relationship context.</returns>
    Task<QueryResult> QueryDataAsync(
        string connectionString,
        string naturalLanguageQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL query and returns the results.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="sqlQuery">SQL query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results.</returns>
    Task<QueryResult> ExecuteQueryAsync(
        string connectionString,
        string sqlQuery,
        CancellationToken cancellationToken = default);
}
