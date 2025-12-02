using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MToGo.WebSocketCustomerService.Services;

/// <summary>
/// Thread-safe registry for customer WebSocket connections.
/// Customers connect to receive real-time updates about their orders.
/// Connection is keyed by customerId - one active connection per customer.
/// </summary>
public class CustomerConnectionManager
{
    private readonly ConcurrentDictionary<int, CustomerConnection> _connections = new();
    private readonly ILogger<CustomerConnectionManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomerConnectionManager(ILogger<CustomerConnectionManager> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Register a customer's WebSocket connection.
    /// If customer already has a connection, the old one is closed and replaced.
    /// </summary>
    public async Task RegisterConnectionAsync(int customerId, WebSocket webSocket)
    {
        var newConnection = new CustomerConnection(webSocket);

        if (_connections.TryGetValue(customerId, out var existingConnection))
        {
            _logger.LogInformation("Customer {CustomerId} opened new connection, closing previous one", customerId);
            await CloseConnectionAsync(existingConnection, "Another session was opened");
        }

        _connections[customerId] = newConnection;
        _logger.LogInformation("Customer {CustomerId} connected. Total connections: {Count}", 
            customerId, _connections.Count);
    }

    /// <summary>
    /// Remove a customer's connection from the registry.
    /// </summary>
    public void RemoveConnection(int customerId)
    {
        if (_connections.TryRemove(customerId, out _))
        {
            _logger.LogInformation("Customer {CustomerId} disconnected. Total connections: {Count}", 
                customerId, _connections.Count);
        }
    }

    /// <summary>
    /// Send a message to a specific customer.
    /// Returns true if message was sent, false if customer not connected.
    /// </summary>
    public async Task<bool> SendToCustomerAsync<T>(int customerId, string eventType, T payload)
    {
        if (!_connections.TryGetValue(customerId, out var connection))
        {
            _logger.LogDebug("Customer {CustomerId} not connected, message not delivered", customerId);
            return false;
        }

        if (connection.WebSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("Customer {CustomerId} WebSocket not open (State: {State}), removing connection", 
                customerId, connection.WebSocket.State);
            RemoveConnection(customerId);
            return false;
        }

        try
        {
            var message = CreateMessage(eventType, payload);
            await connection.WebSocket.SendAsync(
                new ArraySegment<byte>(message),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            _logger.LogInformation("Sent {EventType} to Customer {CustomerId}", eventType, customerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending to Customer {CustomerId}", customerId);
            RemoveConnection(customerId);
            return false;
        }
    }

    /// <summary>
    /// Check if a customer is currently connected.
    /// </summary>
    public bool IsConnected(int customerId)
    {
        if (!_connections.TryGetValue(customerId, out var connection))
            return false;
        return connection.WebSocket.State == WebSocketState.Open;
    }

    public int ConnectionCount => _connections.Count;

    private byte[] CreateMessage<T>(string eventType, T payload)
    {
        var message = new WebSocketMessage<T>
        {
            EventType = eventType,
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    private async Task CloseConnectionAsync(CustomerConnection connection, string reason)
    {
        try
        {
            if (connection.WebSocket.State == WebSocketState.Open)
            {
                await connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error closing WebSocket connection");
        }
    }

    private class CustomerConnection
    {
        public WebSocket WebSocket { get; }
        public DateTime ConnectedAt { get; }

        public CustomerConnection(WebSocket webSocket)
        {
            WebSocket = webSocket;
            ConnectedAt = DateTime.UtcNow;
        }
    }

    private class WebSocketMessage<T>
    {
        public string EventType { get; set; } = string.Empty;
        public T? Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
