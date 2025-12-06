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

    // We keep ServerConnectionOptions support for backward compatibility/legacy injection if needed,
    // but primarily rely on _postgresOptions.ConnectionString
    private ServerConnectionOptions? _serverConnection;
    private readonly object _lock = new();

    public ConnectionBuilderService(
        ILogger<ConnectionBuilderService> logger,
        IOptions<PostgresOptions> postgresOptions)
    {
        _logger = logger;
        _postgresOptions = postgresOptions.Value;
    }

    /// <inheritdoc/>
    public void ConfigureServer(ServerConnectionOptions options)
    {
        lock (_lock)
        {
            _serverConnection = options;
            _serverConnection.IsConfigured = true;
            _logger.LogInformation("Server connection manually configured: {Host}:{Port} as user {Username}",
                options.Host, options.Port, options.Username);
        }
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string database)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Server connection not configured. Please provide 'Postgres:ConnectionString' in configuration or initialize via legacy endpoint.");
        }

        lock (_lock)
        {
            var builder = new NpgsqlConnectionStringBuilder();

            // Priority 1: Configured Connection String
            if (!string.IsNullOrWhiteSpace(_postgresOptions.ConnectionString))
            {
                builder.ConnectionString = _postgresOptions.ConnectionString;
            }
            // Priority 2: Legacy Manual Configuration
            else if (_serverConnection?.IsConfigured == true)
            {
                builder.Host = _serverConnection.Host;
                builder.Port = _serverConnection.Port;
                builder.Username = _serverConnection.Username;
                builder.Password = _serverConnection.Password;
            }

            // Always override/set the requested database
            builder.Database = database;

            // Apply global limits/settings if they weren't already part of the connection string or need enforcing
            // Note: NpgsqlConnectionStringBuilder props override the ConnectionString parsing if set after.
            // We only set them if we want to enforce them from options.
            // However, typical behavior is config string wins, but here we have separate options for timeouts.
            // Let's apply timeouts from options as defaults if not present, but simple approach: set them.

            builder.Timeout = _postgresOptions.ConnectionTimeoutSeconds;
            builder.CommandTimeout = _postgresOptions.CommandTimeoutSeconds;
            builder.MaxPoolSize = _postgresOptions.MaxPoolSize;
            if (_postgresOptions.MinPoolSize > 0)
            {
                builder.MinPoolSize = _postgresOptions.MinPoolSize;
            }

            // SSL handling:
            // If the connection string specifies SSL, we might respect it, but we have a flag UseSsl.
            // If UseSsl is explicitly false, we might want to disable it.
            // If UseSsl is true, we want Prefer/Require.
            if (_postgresOptions.UseSsl)
            {
                 // Only set if not already set effectively by connection string, or enforce it?
                 // Let's enforce it to match previous behavior logic allowing simple config overrides.
                 if (builder.SslMode == SslMode.Disable)
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
                return !string.IsNullOrWhiteSpace(_postgresOptions.ConnectionString) ||
                       _serverConnection?.IsConfigured == true;
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
            // If using connection string, parse it to populate the "view" model
            if (!string.IsNullOrWhiteSpace(_postgresOptions.ConnectionString))
            {
                try
                {
                    var builder = new NpgsqlConnectionStringBuilder(_postgresOptions.ConnectionString);
                    return new ServerConnectionOptions
                    {
                        Host = builder.Host ?? "unknown",
                        Port = builder.Port,
                        Username = builder.Username ?? "unknown",
                        Password = "***", // field masked
                        IsConfigured = true
                    };
                }
                catch
                {
                    // Fallback if parsing fails for some reason
                     return new ServerConnectionOptions
                    {
                        Host = "configured-via-string",
                        Port = 5432,
                        Username = "masked",
                        Password = "***",
                        IsConfigured = true
                    };
                }
            }

            // Legacy fallback
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
}
