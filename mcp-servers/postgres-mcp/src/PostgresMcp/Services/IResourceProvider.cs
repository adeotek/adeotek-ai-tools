using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for providing MCP resources (database connections, schemas, etc.).
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// List all available resources.
    /// </summary>
    /// <param name="cursor">Pagination cursor (optional)</param>
    /// <returns>List of resources and next cursor</returns>
    Task<ListResourcesResult> ListResourcesAsync(string? cursor = null);

    /// <summary>
    /// Read a specific resource by URI.
    /// </summary>
    /// <param name="uri">Resource URI (e.g., "postgres://localhost/mydb/schema")</param>
    /// <returns>Resource contents</returns>
    Task<ReadResourceResult> ReadResourceAsync(string uri);

    /// <summary>
    /// Subscribe to resource changes.
    /// </summary>
    /// <param name="uri">Resource URI to subscribe to</param>
    Task SubscribeAsync(string uri);

    /// <summary>
    /// Unsubscribe from resource changes.
    /// </summary>
    /// <param name="uri">Resource URI to unsubscribe from</param>
    Task UnsubscribeAsync(string uri);

    /// <summary>
    /// Check if resources are available (server is configured).
    /// </summary>
    bool AreResourcesAvailable();
}
