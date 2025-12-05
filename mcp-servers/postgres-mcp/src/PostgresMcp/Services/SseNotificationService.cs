using System.Collections.Concurrent;
using System.Text.Json;

namespace PostgresMcp.Services;

/// <summary>
/// Service for managing Server-Sent Events (SSE) notifications.
/// </summary>
public class SseNotificationService : ISseNotificationService, IDisposable
{
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private readonly ILogger<SseNotificationService> _logger;
    private readonly Timer _heartbeatTimer;
    private readonly JsonSerializerOptions _jsonOptions;

    private record ClientConnection(string ClientId, StreamWriter Writer, CancellationToken CancellationToken);

    public SseNotificationService(ILogger<SseNotificationService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Send heartbeat every 30 seconds to keep connections alive
        _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _logger.LogInformation("SSE notification service initialized");
    }

    public async Task RegisterClientAsync(string clientId, StreamWriter writer, CancellationToken cancellationToken)
    {
        var connection = new ClientConnection(clientId, writer, cancellationToken);

        if (_clients.TryAdd(clientId, connection))
        {
            _logger.LogInformation("SSE client registered: {ClientId}", clientId);

            // Send initial connection message
            await SendEventAsync(writer, "connected", new
            {
                clientId,
                timestamp = DateTime.UtcNow
            }, null);

            // Keep connection alive until cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Normal disconnection
            }
            finally
            {
                await UnregisterClientAsync(clientId);
            }
        }
        else
        {
            _logger.LogWarning("Failed to register SSE client (already exists): {ClientId}", clientId);
        }
    }

    public Task UnregisterClientAsync(string clientId)
    {
        if (_clients.TryRemove(clientId, out _))
        {
            _logger.LogInformation("SSE client unregistered: {ClientId}", clientId);
        }

        return Task.CompletedTask;
    }

    public async Task BroadcastNotificationAsync(string eventType, object data, string? eventId = null)
    {
        var tasks = _clients.Values.Select(client =>
            SendNotificationInternalAsync(client, eventType, data, eventId));

        await Task.WhenAll(tasks);

        _logger.LogDebug("Broadcast notification sent: {EventType} to {ClientCount} clients",
            eventType, _clients.Count);
    }

    public async Task SendNotificationAsync(string clientId, string eventType, object data, string? eventId = null)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            await SendNotificationInternalAsync(client, eventType, data, eventId);
            _logger.LogDebug("Notification sent to client {ClientId}: {EventType}", clientId, eventType);
        }
        else
        {
            _logger.LogWarning("Cannot send notification - client not found: {ClientId}", clientId);
        }
    }

    public async Task SendHeartbeatAsync()
    {
        var disconnectedClients = new List<string>();

        foreach (var client in _clients.Values)
        {
            try
            {
                if (client.CancellationToken.IsCancellationRequested)
                {
                    disconnectedClients.Add(client.ClientId);
                    continue;
                }

                await client.Writer.WriteLineAsync(":heartbeat");
                await client.Writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send heartbeat to client {ClientId}", client.ClientId);
                disconnectedClients.Add(client.ClientId);
            }
        }

        // Clean up disconnected clients
        foreach (var clientId in disconnectedClients)
        {
            await UnregisterClientAsync(clientId);
        }

        if (_clients.Count > 0)
        {
            _logger.LogDebug("Heartbeat sent to {ClientCount} clients", _clients.Count);
        }
    }

    public int GetClientCount()
    {
        return _clients.Count;
    }

    private async Task SendNotificationInternalAsync(
        ClientConnection client,
        string eventType,
        object data,
        string? eventId)
    {
        try
        {
            if (client.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            await SendEventAsync(client.Writer, eventType, data, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to client {ClientId}", client.ClientId);
            await UnregisterClientAsync(client.ClientId);
        }
    }

    private async Task SendEventAsync(StreamWriter writer, string eventType, object data, string? eventId)
    {
        // SSE format:
        // id: <event-id>
        // event: <event-type>
        // data: <json-data>
        // (blank line)

        if (!string.IsNullOrEmpty(eventId))
        {
            await writer.WriteLineAsync($"id: {eventId}");
        }

        await writer.WriteLineAsync($"event: {eventType}");

        var jsonData = JsonSerializer.Serialize(data, _jsonOptions);
        await writer.WriteLineAsync($"data: {jsonData}");

        await writer.WriteLineAsync(); // Blank line to end event
        await writer.FlushAsync();
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
        _clients.Clear();
        GC.SuppressFinalize(this);
    }
}
