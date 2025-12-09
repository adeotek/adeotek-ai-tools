using AdeotekSqlMcp.Models;

namespace AdeotekSqlMcp.Database;

/// <summary>
/// Database interface for abstract database operations
/// </summary>
public interface IDatabase : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Database type ("mssql" or "postgres")
    /// </summary>
    string DatabaseType { get; }

    /// <summary>
    /// Tests the database connection
    /// </summary>
    Task TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all databases on the server
    /// </summary>
    Task<IReadOnlyList<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all tables in a database
    /// </summary>
    Task<IReadOnlyList<TableInfo>> ListTablesAsync(string database, string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed schema information for a table
    /// </summary>
    Task<TableSchema> DescribeTableAsync(string database, string schema, string table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SELECT query
    /// </summary>
    Task<QueryResult> ExecuteQueryAsync(string database, string query, int maxRows = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the query execution plan
    /// </summary>
    Task<string> GetQueryPlanAsync(string database, string query, CancellationToken cancellationToken = default);
}
