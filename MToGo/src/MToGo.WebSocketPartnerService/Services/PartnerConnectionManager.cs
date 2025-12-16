using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MToGo.WebSocketPartnerService.Models;

namespace MToGo.WebSocketPartnerService.Services;

// Thread-safe registry for PartnerId -> WebSocket connection
// Latest-connection wins, so no duplicate connections from the same partner
public class PartnerConnectionManager
{
    private readonly ConcurrentDictionary<int, PartnerConnection> _connections = new();
    private readonly ILogger<PartnerConnectionManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PartnerConnectionManager(ILogger<PartnerConnectionManager> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // Closes old connection if partner reconnects (new tab, refresh, etc.)
    public async Task RegisterConnectionAsync(int partnerId, WebSocket webSocket)
    {
        var newConnection = new PartnerConnection(webSocket);

        // If partner already connected, close old connection (latest-wins)
        if (_connections.TryGetValue(partnerId, out var existingConnection))
        {
            _logger.LogInformation("Partner {PartnerId} opened new connection, closing previous one", partnerId);
            await CloseConnectionAsync(existingConnection, "Another session was opened");
        }

        _connections[partnerId] = newConnection;
        _logger.LogInformation("Partner {PartnerId} connected. Total connections: {Count}", partnerId, _connections.Count);
    }

    public void RemoveConnection(int partnerId)
    {
        if (_connections.TryRemove(partnerId, out _))
        {
            _logger.LogInformation("Partner {PartnerId} disconnected. Total connections: {Count}", partnerId, _connections.Count);
        }
    }

    // Returns false if partner isn't connected
    public async Task<bool> SendToPartnerAsync<T>(int partnerId, string eventType, T payload)
    {
        if (!_connections.TryGetValue(partnerId, out var connection))
        {
            _logger.LogDebug("Partner {PartnerId} not connected, message not delivered", partnerId);
            return false;
        }

        if (connection.WebSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("Partner {PartnerId} WebSocket not open (State: {State}), removing connection", 
                partnerId, connection.WebSocket.State);
            RemoveConnection(partnerId);
            return false;
        }

        try
        {
            var message = new WebSocketMessage<T>
            {
                EventType = eventType,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            await connection.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);

            _logger.LogInformation("Sent {EventType} to Partner {PartnerId}", eventType, partnerId);
            return true;
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "Failed to send message to Partner {PartnerId}", partnerId);
            RemoveConnection(partnerId);
            return false;
        }
    }

    public bool IsConnected(int partnerId)
    {
        return _connections.TryGetValue(partnerId, out var conn) 
               && conn.WebSocket.State == WebSocketState.Open;
    }

    public int ConnectionCount => _connections.Count;

    private async Task CloseConnectionAsync(PartnerConnection connection, string reason)
    {
        try
        {
            if (connection.WebSocket.State == WebSocketState.Open)
            {
                // Send close message with reason
                var closeMessage = new { type = "connection_closed", reason };
                var json = JsonSerializer.Serialize(closeMessage, _jsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json);

                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);

                await connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing old WebSocket connection");
        }
    }
}
