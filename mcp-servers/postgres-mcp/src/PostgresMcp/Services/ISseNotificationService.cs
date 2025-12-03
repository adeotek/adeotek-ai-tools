using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for managing Server-Sent Events (SSE) notifications to connected clients.
/// </summary>
public interface ISseNotificationService
{
    /// <summary>
    /// Register a new SSE client connection.
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="writer">StreamWriter for sending events to the client</param>
    /// <param name="cancellationToken">Cancellation token for the connection</param>
    Task RegisterClientAsync(string clientId, StreamWriter writer, CancellationToken cancellationToken);

    /// <summary>
    /// Unregister an SSE client connection.
    /// </summary>
    /// <param name="clientId">Client identifier to remove</param>
    Task UnregisterClientAsync(string clientId);

    /// <summary>
    /// Send a notification to all connected clients.
    /// </summary>
    /// <param name="eventType">Type of event (e.g., "resources/updated")</param>
    /// <param name="data">Event data (will be serialized to JSON)</param>
    /// <param name="eventId">Optional event ID for client tracking</param>
    Task BroadcastNotificationAsync(string eventType, object data, string? eventId = null);

    /// <summary>
    /// Send a notification to a specific client.
    /// </summary>
    /// <param name="clientId">Target client identifier</param>
    /// <param name="eventType">Type of event</param>
    /// <param name="data">Event data</param>
    /// <param name="eventId">Optional event ID</param>
    Task SendNotificationAsync(string clientId, string eventType, object data, string? eventId = null);

    /// <summary>
    /// Send a heartbeat/ping to all connected clients.
    /// </summary>
    Task SendHeartbeatAsync();

    /// <summary>
    /// Get the number of connected clients.
    /// </summary>
    int GetClientCount();
}
