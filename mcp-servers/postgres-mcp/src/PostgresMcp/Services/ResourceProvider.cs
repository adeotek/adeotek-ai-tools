using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Provides MCP resources for PostgreSQL databases.
/// </summary>
public class ResourceProvider : IResourceProvider
{
    private readonly IConnectionBuilderService _connectionBuilder;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISseNotificationService _sseService;
    private readonly ILogger<ResourceProvider> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _subscriptions = new();

    public ResourceProvider(
        IConnectionBuilderService connectionBuilder,
        IServiceScopeFactory serviceScopeFactory,
        ISseNotificationService sseService,
        ILogger<ResourceProvider> logger)
    {
        _connectionBuilder = connectionBuilder;
        _serviceScopeFactory = serviceScopeFactory;
        _sseService = sseService;
        _logger = logger;
    }

    public Task<ListResourcesResult> ListResourcesAsync(string? cursor = null)
    {
        if (!_connectionBuilder.IsConfigured)
        {
            return Task.FromResult(new ListResourcesResult
            {
                Resources = [],
                NextCursor = null
            });
        }

        var config = _connectionBuilder.GetServerConfiguration();
        var resources = new List<Resource>
        {
            new()
            {
                Uri = $"postgres://{config.Host}:{config.Port}/connection",
                Name = "PostgreSQL Connection",
                Description = $"Connection to PostgreSQL server at {config.Host}:{config.Port}",
                MimeType = "application/json"
            },
            new()
            {
                Uri = $"postgres://{config.Host}:{config.Port}/databases",
                Name = "Available Databases",
                Description = "List of databases on the PostgreSQL server",
                MimeType = "application/json"
            }
        };

        // Pagination (simple implementation - in production you'd want more sophisticated pagination)
        var pageSize = 50;
        var startIndex = string.IsNullOrEmpty(cursor) ? 0 : int.Parse(cursor);
        var pagedResources = resources.Skip(startIndex).Take(pageSize).ToList();
        var nextCursor = startIndex + pagedResources.Count < resources.Count
            ? (startIndex + pagedResources.Count).ToString()
            : null;

        return Task.FromResult(new ListResourcesResult
        {
            Resources = pagedResources,
            NextCursor = nextCursor
        });
    }

    public async Task<ReadResourceResult> ReadResourceAsync(string uri)
    {
        if (!_connectionBuilder.IsConfigured)
        {
            throw new InvalidOperationException("Server not configured. Call initialize first.");
        }

        _logger.LogInformation("Reading resource: {Uri}", uri);

        // Parse URI: postgres://host:port/type or postgres://host:port/database/schema
        var uriParts = uri.Replace("postgres://", "").Split('/');

        if (uriParts.Length < 2)
        {
            throw new ArgumentException($"Invalid resource URI: {uri}");
        }

        var resourceType = uriParts.Length >= 2 ? uriParts[1] : string.Empty;

        return resourceType switch
        {
            "connection" => await GetConnectionResourceAsync(uri),
            "databases" => await GetDatabasesResourceAsync(uri),
            "schema" when uriParts.Length >= 3 => await GetSchemaResourceAsync(uri, uriParts[2]),
            _ => throw new ArgumentException($"Unknown resource type in URI: {uri}")
        };
    }

    public Task SubscribeAsync(string uri)
    {
        _subscriptions.TryAdd(uri, DateTime.UtcNow);
        _logger.LogInformation("Subscribed to resource: {Uri}", uri);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string uri)
    {
        _subscriptions.TryRemove(uri, out _);
        _logger.LogInformation("Unsubscribed from resource: {Uri}", uri);
        return Task.CompletedTask;
    }

    public bool AreResourcesAvailable()
    {
        return _connectionBuilder.IsConfigured;
    }

    private Task<ReadResourceResult> GetConnectionResourceAsync(string uri)
    {
        var config = _connectionBuilder.GetServerConfiguration();
        var connectionInfo = new
        {
            host = config.Host,
            port = config.Port,
            username = config.Username,
            status = "connected",
            timestamp = DateTime.UtcNow
        };

        return Task.FromResult(new ReadResourceResult
        {
            Contents =
            [
                new ResourceContents
                {
                    Uri = uri,
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(connectionInfo, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })
                }
            ]
        });
    }

    private Task<ReadResourceResult> GetDatabasesResourceAsync(string uri)
    {
        // In a real implementation, you'd query the PostgreSQL server for available databases
        // For now, return a placeholder
        var databases = new
        {
            databases = new[] { "postgres", "template1", "template0" },
            timestamp = DateTime.UtcNow
        };

        return Task.FromResult(new ReadResourceResult
        {
            Contents =
            [
                new ResourceContents
                {
                    Uri = uri,
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(databases, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })
                }
            ]
        });
    }

    private async Task<ReadResourceResult> GetSchemaResourceAsync(string uri, string database)
    {
        var connectionString = _connectionBuilder.BuildConnectionString(database);

        // Create a scope to resolve the scoped IDatabaseSchemaService
        using var scope = _serviceScopeFactory.CreateScope();
        var schemaService = scope.ServiceProvider.GetRequiredService<IDatabaseSchemaService>();
        var schema = await schemaService.ScanDatabaseSchemaAsync(connectionString);

        return new ReadResourceResult
        {
            Contents =
            [
                new ResourceContents
                {
                    Uri = uri,
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(schema, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })
                }
            ]
        };
    }
}
