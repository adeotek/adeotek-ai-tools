using PostgresNaturalLanguageMcp.Models;

namespace PostgresNaturalLanguageMcp.Services;

/// <summary>
/// Service for generating SQL queries from natural language using AI.
/// </summary>
public interface ISqlGenerationService
{
    /// <summary>
    /// Generates and executes a SQL query from a natural language description.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="naturalLanguageQuery">Natural language description of the desired query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated SQL query, explanation, and results.</returns>
    Task<SqlGenerationResult> GenerateAndExecuteQueryAsync(
        string connectionString,
        string naturalLanguageQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a SQL query for safety.
    /// </summary>
    /// <param name="sqlQuery">SQL query to validate.</param>
    /// <returns>True if the query is safe to execute.</returns>
    bool ValidateSqlSafety(string sqlQuery);

    /// <summary>
    /// Optimizes a SQL query.
    /// </summary>
    /// <param name="sqlQuery">SQL query to optimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Optimized SQL query with suggestions.</returns>
    Task<string> OptimizeQueryAsync(
        string sqlQuery,
        CancellationToken cancellationToken = default);
}
