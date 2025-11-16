using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WebsocketPartnerService.Services;

/// <summary>
/// Manages WebSocket connections for restaurant partners.
/// This is a SIMPLIFIED example for demo purposes.
/// 
/// TODO for production:
/// - Add authentication/authorization
/// - Implement reconnection logic
/// - Add heartbeat/ping-pong for connection health
/// - Handle connection failures gracefully
/// - Scale with Redis or similar for multi-instance deployments
/// </summary>
public class WebSocketConnectionManager
{
    // Store active WebSocket connections per restaurant
    private readonly ConcurrentDictionary<string, List<WebSocket>> _connections = new();

    /// <summary>
    /// Handles a WebSocket connection from a restaurant client.
    /// Keeps the connection alive and handles incoming messages.
    /// </summary>
    public async Task HandleWebSocketConnection(string restaurantId, WebSocket webSocket)
    {
        Console.WriteLine($"🔌 Restaurant '{restaurantId}' connected via WebSocket");

        // Add connection to the pool
        _connections.AddOrUpdate(
            restaurantId,
            new List<WebSocket> { webSocket },
            (key, existingList) =>
            {
                existingList.Add(webSocket);
                return existingList;
            }
        );

        // Send welcome message
        await SendMessageToWebSocket(webSocket, new
        {
            type = "connection",
            message = "Connected to WebSocketPartnerService",
            restaurantId = restaurantId,
            timestamp = DateTime.UtcNow
        });

        // Keep connection alive and listen for messages
        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Closing", 
                        CancellationToken.None
                    );
                }
                else
                {
                    // Echo back or handle client messages if needed
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📨 Received from {restaurantId}: {message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket error for {restaurantId}: {ex.Message}");
        }
        finally
        {
            // Remove connection when closed
            if (_connections.TryGetValue(restaurantId, out var connections))
            {
                connections.Remove(webSocket);
                if (connections.Count == 0)
                {
                    _connections.TryRemove(restaurantId, out _);
                }
            }
            Console.WriteLine($"🔌 Restaurant '{restaurantId}' disconnected");
        }
    }

    /// <summary>
    /// Broadcasts a message to all connected restaurants.
    /// Used when a new order is created (via Kafka event).
    /// </summary>
    public async Task BroadcastToAllRestaurants(object message)
    {
        Console.WriteLine($"📢 Broadcasting to {_connections.Count} restaurant(s)...");

        foreach (var restaurantConnections in _connections.Values)
        {
            foreach (var webSocket in restaurantConnections.ToList())
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await SendMessageToWebSocket(webSocket, message);
                }
            }
        }
    }

    /// <summary>
    /// Sends a message to a specific restaurant.
    /// </summary>
    public async Task SendToRestaurant(string restaurantId, object message)
    {
        if (_connections.TryGetValue(restaurantId, out var connections))
        {
            Console.WriteLine($"📤 Sending message to restaurant '{restaurantId}'");
            
            foreach (var webSocket in connections.ToList())
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await SendMessageToWebSocket(webSocket, message);
                }
            }
        }
        else
        {
            Console.WriteLine($"⚠️  Restaurant '{restaurantId}' not connected");
        }
    }

    /// <summary>
    /// Helper method to send JSON message through WebSocket.
    /// </summary>
    private async Task SendMessageToWebSocket(WebSocket webSocket, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var arraySegment = new ArraySegment<byte>(bytes);

        await webSocket.SendAsync(
            arraySegment,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Gets the count of currently connected restaurants.
    /// </summary>
    public int GetConnectedRestaurantCount() => _connections.Count;
}
