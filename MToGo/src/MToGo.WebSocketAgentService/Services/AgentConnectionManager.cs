using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MToGo.WebSocketAgentService.Services;

// Thread-safe registry for agent WebSocket connections
// Supports two connection types:
// 1. Broadcast connections: All agents listening for available jobs (/agents)
// 2. Individual connections: Specific agent receiving updates on their orders (/agents/{id})
public class AgentConnectionManager
{
    // Agents in the "available jobs" broadcast room
    private readonly ConcurrentDictionary<string, AgentConnection> _broadcastConnections = new();
    
    // Individual agent connections for personal updates (keyed by agentId)
    private readonly ConcurrentDictionary<int, AgentConnection> _agentConnections = new();
    
    private readonly ILogger<AgentConnectionManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentConnectionManager(ILogger<AgentConnectionManager> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // Register agent to receive broadcast messages (available jobs)
    // Uses a unique connection ID since same agent could have multiple tabs
    public Task RegisterBroadcastConnectionAsync(string connectionId, WebSocket webSocket)
    {
        var connection = new AgentConnection(webSocket);
        _broadcastConnections[connectionId] = connection;
        _logger.LogInformation("Agent joined broadcast room. ConnectionId={ConnectionId}, Total broadcast connections: {Count}", 
            connectionId, _broadcastConnections.Count);
        return Task.CompletedTask;
    }

    public void RemoveBroadcastConnection(string connectionId)
    {
        if (_broadcastConnections.TryRemove(connectionId, out _))
        {
            _logger.LogInformation("Agent left broadcast room. ConnectionId={ConnectionId}, Total broadcast connections: {Count}", 
                connectionId, _broadcastConnections.Count);
        }
    }

    // Register agent for personal updates (order ready notifications, etc.)
    // Latest connection wins - if agent opens new tab, old one is replaced
    public async Task RegisterAgentConnectionAsync(int agentId, WebSocket webSocket)
    {
        var newConnection = new AgentConnection(webSocket);

        if (_agentConnections.TryGetValue(agentId, out var existingConnection))
        {
            _logger.LogInformation("Agent {AgentId} opened new connection, closing previous one", agentId);
            await CloseConnectionAsync(existingConnection, "Another session was opened");
        }

        _agentConnections[agentId] = newConnection;
        _logger.LogInformation("Agent {AgentId} registered for personal updates. Total agent connections: {Count}", 
            agentId, _agentConnections.Count);
    }

    public void RemoveAgentConnection(int agentId)
    {
        if (_agentConnections.TryRemove(agentId, out _))
        {
            _logger.LogInformation("Agent {AgentId} disconnected. Total agent connections: {Count}", 
                agentId, _agentConnections.Count);
        }
    }

    // Send to ALL agents in the broadcast room (new job available, job taken, etc.)
    public async Task BroadcastToAllAgentsAsync<T>(string eventType, T payload)
    {
        var message = CreateMessage(eventType, payload);
        var deadConnections = new List<string>();

        foreach (var (connectionId, connection) in _broadcastConnections)
        {
            if (connection.WebSocket.State != WebSocketState.Open)
            {
                deadConnections.Add(connectionId);
                continue;
            }

            try
            {
                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send to broadcast connection {ConnectionId}", connectionId);
                deadConnections.Add(connectionId);
            }
        }

        // Clean up dead connections
        foreach (var connectionId in deadConnections)
        {
            RemoveBroadcastConnection(connectionId);
        }

        _logger.LogInformation("Broadcast {EventType} to {Count} agents", eventType, _broadcastConnections.Count);
    }

    // Send to a specific agent (order ready notification)
    public async Task<bool> SendToAgentAsync<T>(int agentId, string eventType, T payload)
    {
        if (!_agentConnections.TryGetValue(agentId, out var connection))
        {
            _logger.LogDebug("Agent {AgentId} not connected, message not delivered", agentId);
            return false;
        }

        if (connection.WebSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("Agent {AgentId} WebSocket not open (State: {State}), removing connection", 
                agentId, connection.WebSocket.State);
            RemoveAgentConnection(agentId);
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

            _logger.LogInformation("Sent {EventType} to Agent {AgentId}", eventType, agentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending to Agent {AgentId}", agentId);
            RemoveAgentConnection(agentId);
            return false;
        }
    }

    public bool IsAgentConnected(int agentId)
    {
        if (!_agentConnections.TryGetValue(agentId, out var connection))
            return false;
        return connection.WebSocket.State == WebSocketState.Open;
    }

    public int BroadcastConnectionCount => _broadcastConnections.Count;
    public int AgentConnectionCount => _agentConnections.Count;

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

    private async Task CloseConnectionAsync(AgentConnection connection, string reason)
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

    private class AgentConnection
    {
        public WebSocket WebSocket { get; }
        public DateTime ConnectedAt { get; }

        public AgentConnection(WebSocket webSocket)
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
