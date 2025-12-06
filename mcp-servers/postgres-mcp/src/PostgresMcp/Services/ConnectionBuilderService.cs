using Microsoft.Extensions.Options;
using Npgsql;
using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for building PostgreSQL connection strings from server configuration and database names.
/// </summary>
public class ConnectionBuilderService : IConnectionBuilderService
{
    private readonly ILogger<ConnectionBuilderService> _logger;
    private readonly PostgresOptions _postgresOptions;
    private ServerConnectionOptions? _serverConnection;
    private readonly object _lock = new();

    public ConnectionBuilderService(
        ILogger<ConnectionBuilderService> logger,
        IOptions<PostgresOptions> postgresOptions)
    {
        _logger = logger;
        _postgresOptions = postgresOptions.Value;

        // Auto-configure from connection string if provided in configuration
        if (!string.IsNullOrWhiteSpace(_postgresOptions.ConnectionString))
        {
            try
            {
                InitializeFromConnectionString(_postgresOptions.ConnectionString);
                _logger.LogInformation("Server connection configured from appsettings connection string");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize from connection string in configuration. Connection can still be configured at runtime.");
            }
        }
    }

    /// <inheritdoc/>
    public void ConfigureServer(ServerConnectionOptions options)
    {
        lock (_lock)
        {
            _serverConnection = options;
            _serverConnection.IsConfigured = true;
            _logger.LogInformation("Server connection configured: {Host}:{Port} as user {Username}",
                options.Host, options.Port, options.Username);
        }
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string database)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Server connection not configured. Please initialize the MCP server with connection parameters.");
        }

        lock (_lock)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = _serverConnection!.Host,
                Port = _serverConnection.Port,
                Username = _serverConnection.Username,
                Password = _serverConnection.Password,
                Database = database,
                Timeout = _postgresOptions.ConnectionTimeoutSeconds,
                CommandTimeout = _postgresOptions.CommandTimeoutSeconds,
                MaxPoolSize = _postgresOptions.MaxPoolSize,
                MinPoolSize = _postgresOptions.MinPoolSize
            };

            // Configure SSL
            if (_postgresOptions.UseSsl)
            {
                builder.SslMode = SslMode.Prefer;
            }
            else
            {
                builder.SslMode = SslMode.Disable;
            }

            _logger.LogDebug("Built connection string for database: {Database}", database);
            return builder.ConnectionString;
        }
    }

    /// <inheritdoc/>
    public bool IsConfigured
    {
        get
        {
            lock (_lock)
            {
                return _serverConnection?.IsConfigured == true;
            }
        }
    }

    /// <inheritdoc/>
    public ServerConnectionOptions GetServerConfiguration()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Server connection not configured.");
        }

        lock (_lock)
        {
            // Return a copy without the password for security
            return new ServerConnectionOptions
            {
                Host = _serverConnection!.Host,
                Port = _serverConnection.Port,
                Username = _serverConnection.Username,
                Password = "***",
                IsConfigured = _serverConnection.IsConfigured
            };
        }
    }

    /// <summary>
    /// Initialize server connection from a connection string.
    /// The connection string can include or omit the Database parameter.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <exception cref="ArgumentException">Thrown when required connection parameters are missing</exception>
    private void InitializeFromConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new ArgumentException("Connection string must specify a Host");
        }

        if (string.IsNullOrWhiteSpace(builder.Username))
        {
            throw new ArgumentException("Connection string must specify a Username");
        }

        var options = new ServerConnectionOptions
        {
            Host = builder.Host,
            Port = builder.Port,
            Username = builder.Username,
            Password = builder.Password ?? string.Empty,
            UseSsl = builder.SslMode != SslMode.Disable,
            IsConfigured = true
        };

        lock (_lock)
        {
            _serverConnection = options;
            _logger.LogDebug("Server connection initialized from connection string: {Host}:{Port} as user {Username}",
                options.Host, options.Port, options.Username);
        }
    }
}
