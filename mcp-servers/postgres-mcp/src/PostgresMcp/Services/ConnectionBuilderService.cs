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
    private NpgsqlConnectionStringBuilder? _baseConnectionBuilder;
    private readonly object _lock = new();

    public ConnectionBuilderService(
        ILogger<ConnectionBuilderService> logger,
        IOptions<PostgresOptions> postgresOptions)
    {
        _logger = logger;
        _postgresOptions = postgresOptions.Value;

        // Initialize from configuration if connection string is provided
        if (!string.IsNullOrWhiteSpace(_postgresOptions.ConnectionString))
        {
            try
            {
                _baseConnectionBuilder = new NpgsqlConnectionStringBuilder(_postgresOptions.ConnectionString);

                // Apply additional options from configuration
                ApplyConnectionOptions(_baseConnectionBuilder);

                _logger.LogInformation("Server connection configured from configuration: {Host}:{Port} as user {Username}",
                    _baseConnectionBuilder.Host, _baseConnectionBuilder.Port, _baseConnectionBuilder.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse connection string from configuration. Server will need to be configured via initialize method.");
                _baseConnectionBuilder = null;
            }
        }
    }

    /// <inheritdoc/>
    public void ConfigureServer(ServerConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new ArgumentException("Username and password are required", nameof(options));
        }

        lock (_lock)
        {
            _serverConnection = options;
            _serverConnection.IsConfigured = true;

            // Build base connection from server options
            _baseConnectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port,
                Username = options.Username,
                Password = options.Password,
                Database = "postgres" // Default database
            };

            // Apply additional options from configuration
            ApplyConnectionOptions(_baseConnectionBuilder);

            // Configure SSL from server options (takes precedence)
            _baseConnectionBuilder.SslMode = options.UseSsl ? SslMode.Prefer : SslMode.Disable;

            _logger.LogInformation("Server connection configured via runtime: {Host}:{Port} as user {Username}",
                options.Host, options.Port, options.Username);
        }
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string database)
    {
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException("Database name cannot be null or empty", nameof(database));
        }

        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Server connection not configured. Configure via appsettings.json (Postgres:ConnectionString) or call initialize with connection parameters.");
        }

        lock (_lock)
        {
            // Clone the base connection builder to avoid modifying the original
            var builder = new NpgsqlConnectionStringBuilder(_baseConnectionBuilder!.ConnectionString)
            {
                Database = database
            };

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
                return _baseConnectionBuilder != null;
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
                Host = _baseConnectionBuilder!.Host ?? "localhost",
                Port = _baseConnectionBuilder.Port,
                Username = _baseConnectionBuilder.Username ?? string.Empty,
                Password = "***",
                UseSsl = _baseConnectionBuilder.SslMode != SslMode.Disable,
                IsConfigured = true
            };
        }
    }

    /// <summary>
    /// Apply connection pool and timeout options from configuration to the connection builder.
    /// </summary>
    /// <param name="builder">The connection string builder to configure</param>
    private void ApplyConnectionOptions(NpgsqlConnectionStringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Timeout = _postgresOptions.ConnectionTimeoutSeconds;
        builder.CommandTimeout = _postgresOptions.CommandTimeoutSeconds;
        builder.MaxPoolSize = _postgresOptions.MaxPoolSize;
        builder.MinPoolSize = _postgresOptions.MinPoolSize;

        // Configure SSL if not already set and UseSsl is configured
        if (builder.SslMode == SslMode.Disable && _postgresOptions.UseSsl)
        {
            builder.SslMode = SslMode.Prefer;
        }
    }
}
