using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Utilities;

namespace AdeotekSqlMcp.Database;

/// <summary>
/// Parses connection strings in the format: type=mssql;host=localhost;port=1433;user=sa;password=pass;database=mydb
/// </summary>
public static class ConnectionStringParser
{
    /// <summary>
    /// Parses a connection string into DatabaseConfig
    /// </summary>
    public static DatabaseConfig Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ConfigurationException("Connection string cannot be empty");
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                config[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        // Extract type (required)
        if (!config.TryGetValue("type", out var type))
        {
            throw new ConfigurationException("Connection string must include 'type' (mssql or postgres)");
        }

        type = type.ToLowerInvariant();
        if (type != "mssql" && type != "postgres")
        {
            throw new ConfigurationException($"Invalid database type: {type}. Must be 'mssql' or 'postgres'");
        }

        // Extract host (required)
        if (!TryGetValue(config, out var host, "host", "server", "data source"))
        {
            throw new ConfigurationException("Connection string must include 'host'");
        }

        // Extract port (optional, defaults based on type)
        var defaultPort = type == "mssql" ? 1433 : 5432;
        var port = TryGetValue(config, out var portStr, "port")
            ? int.Parse(portStr)
            : defaultPort;

        // Extract database (optional)
        TryGetValue(config, out var database, "database", "initial catalog");
        database ??= type == "mssql" ? "master" : "postgres";

        // Extract user (required)
        if (!TryGetValue(config, out var user, "user", "username", "user id", "uid"))
        {
            throw new ConfigurationException("Connection string must include 'user'");
        }

        // Extract password (required)
        if (!TryGetValue(config, out var password, "password", "pwd"))
        {
            throw new ConfigurationException("Connection string must include 'password'");
        }

        // Extract SSL/TLS setting (optional)
        var useSsl = TryGetValue(config, out var sslStr, "ssl", "encrypt") &&
                     (sslStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      sslStr.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                      sslStr.Equals("1", StringComparison.OrdinalIgnoreCase));

        // Extract timeouts (optional)
        var connectionTimeout = TryGetValue(config, out var connTimeoutStr, "connectiontimeout", "connect timeout")
            ? int.Parse(connTimeoutStr)
            : 30;

        var commandTimeout = TryGetValue(config, out var cmdTimeoutStr, "commandtimeout", "request timeout")
            ? int.Parse(cmdTimeoutStr)
            : 30;

        return new DatabaseConfig
        {
            Type = type,
            Host = host,
            Port = port,
            Database = database,
            User = user,
            Password = password,
            UseSsl = useSsl,
            ConnectionTimeout = connectionTimeout,
            CommandTimeout = commandTimeout
        };
    }

    private static bool TryGetValue(Dictionary<string, string> config, out string? value, params string[] keys)
    {
        value = null;
        foreach (var key in keys)
        {
            if (config.TryGetValue(key, out value))
            {
                return true;
            }
        }
        return false;
    }
}
