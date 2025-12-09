using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Utilities;
using Serilog.Core;

namespace AdeotekSqlMcp.Database;

/// <summary>
/// Factory for creating database instances
/// </summary>
public static class DatabaseFactory
{
    /// <summary>
    /// Creates a database instance based on connection string
    /// </summary>
    public static IDatabase Create(string connectionString, Logger logger)
    {
        var config = ConnectionStringParser.Parse(connectionString);
        return Create(config, logger);
    }

    /// <summary>
    /// Creates a database instance based on configuration
    /// </summary>
    public static IDatabase Create(DatabaseConfig config, Logger logger)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "mssql" => new SqlServerDatabase(config, logger),
            "postgres" => new PostgresDatabase(config, logger),
            _ => throw new ConfigurationException($"Unsupported database type: {config.Type}")
        };
    }
}
