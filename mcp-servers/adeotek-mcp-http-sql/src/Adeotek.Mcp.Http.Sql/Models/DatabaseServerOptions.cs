using Microsoft.Data.SqlClient;
using Npgsql;

namespace Adeotek.Mcp.Http.Sql.Models;

public enum DatabaseServerType
{
    Postgres,
    MsSql
}

/// <summary>
/// Database server connection settings.
/// </summary>
public class DatabaseServerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DatabaseServer";

    /// <summary>
    /// Database server type.
    /// Current supported types:
    /// - "postgres"
    /// - "mssql"
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Database connection string template.
    /// The database name can be omitted or set to a default, as tools will specify the database to use.
    /// Example: "Host=localhost;Port=5432;Username=postgres;Password=;Database=postgres"
    /// </summary>
    public string? ConnectionString { get; set; }

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

    /// <summary>
    /// Whether to log SQL queries.
    /// </summary>
    public bool LogQueries { get; set; }

    /// <summary>
    /// Whether to log query results.
    /// </summary>
    public bool LogResults { get; set; }

    public DatabaseServerType DbServerType =>
        !string.IsNullOrEmpty(Type) && Enum.TryParse<DatabaseServerType>(Type, true, out var dbType)
            ? dbType
            : throw new InvalidOperationException($"Unsupported Database Server Type: {Type}");

    public string GetConnectionStringForDatabase(string? databaseName)
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString is not configured.");
        }

        string connectionString;
        switch (DbServerType)
        {
            case DatabaseServerType.Postgres:
                var pgConnectionBuilder = new NpgsqlConnectionStringBuilder(ConnectionString)
                {
                    Database = string.IsNullOrEmpty(databaseName)
                        ? "postgres"
                        : databaseName
                };
                connectionString = pgConnectionBuilder.ToString();
                break;
            case DatabaseServerType.MsSql:
                var msConnectionBuilder = new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = string.IsNullOrEmpty(databaseName)
                        ? "master"
                        : databaseName
                };
                connectionString = msConnectionBuilder.ToString();
                break;
            default:
                throw new InvalidOperationException($"Unsupported Database Server Type: {DbServerType}");
        }

        return connectionString;
    }
}
