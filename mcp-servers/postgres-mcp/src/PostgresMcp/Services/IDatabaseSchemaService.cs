using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for scanning and analyzing PostgreSQL database schema.
/// </summary>
public interface IDatabaseSchemaService
{
    /// <summary>
    /// Scans the database schema and returns comprehensive structure information.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="schemaFilter">Optional schema filter (e.g., "public").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Database schema information.</returns>
    Task<DatabaseSchema> ScanDatabaseSchemaAsync(
        string connectionString,
        string? schemaFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific table.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="schemaName">Schema name.</param>
    /// <param name="tableName">Table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Table information.</returns>
    Task<TableInfo?> GetTableInfoAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a natural language question about the database schema.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="question">Natural language question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Answer to the question.</returns>
    Task<string> AnswerSchemaQuestionAsync(
        string connectionString,
        string question,
        CancellationToken cancellationToken = default);
}
