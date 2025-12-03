using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for building PostgreSQL connection strings.
/// </summary>
public interface IConnectionBuilderService
{
    /// <summary>
    /// Configure the server connection parameters.
    /// </summary>
    /// <param name="options">Server connection options</param>
    void ConfigureServer(ServerConnectionOptions options);

    /// <summary>
    /// Build a connection string for a specific database.
    /// </summary>
    /// <param name="database">Database name</param>
    /// <returns>Complete connection string</returns>
    string BuildConnectionString(string database);

    /// <summary>
    /// Check if server connection is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Get the current server configuration.
    /// </summary>
    ServerConnectionOptions GetServerConfiguration();
}
