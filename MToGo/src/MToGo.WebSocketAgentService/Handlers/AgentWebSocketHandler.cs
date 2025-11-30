using System.Net.WebSockets;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.Handlers;

// Handles WebSocket connections for agents
// Two endpoint types:
// - /agents (broadcast room): All agents see available jobs
// - /agents/{id} (personal room): Specific agent gets order updates
public class AgentWebSocketHandler
{
    private readonly AgentConnectionManager _connectionManager;
    private readonly ILogger<AgentWebSocketHandler> _logger;

    public AgentWebSocketHandler(
        AgentConnectionManager connectionManager,
        ILogger<AgentWebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    // Handle broadcast room connection (/agents)
    // All connected agents receive new job notifications
    public async Task HandleBroadcastConnectionAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
            return;
        }

        var connectionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Agent connecting to broadcast room. ConnectionId={ConnectionId}", connectionId);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await _connectionManager.RegisterBroadcastConnectionAsync(connectionId, webSocket);

        try
        {
            await KeepConnectionAliveAsync(webSocket);
        }
        finally
        {
            _connectionManager.RemoveBroadcastConnection(connectionId);
            _logger.LogInformation("Agent left broadcast room. ConnectionId={ConnectionId}", connectionId);
        }
    }

    // Handle personal connection (/agents/{id})
    // Agent receives updates on their assigned orders
    public async Task HandleAgentConnectionAsync(HttpContext context, int agentId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
            return;
        }

        _logger.LogInformation("Agent {AgentId} connecting for personal updates", agentId);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await _connectionManager.RegisterAgentConnectionAsync(agentId, webSocket);

        try
        {
            await KeepConnectionAliveAsync(webSocket);
        }
        finally
        {
            _connectionManager.RemoveAgentConnection(agentId);
            _logger.LogInformation("Agent {AgentId} disconnected from personal room", agentId);
        }
    }

    // Keep WebSocket alive until client disconnects
    private async Task KeepConnectionAliveAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client requested close",
                        CancellationToken.None);
                    break;
                }

                // We don't expect clients to send messages, but log if they do
                if (result.Count > 0)
                {
                    _logger.LogDebug("Received {Count} bytes from client (ignored)", result.Count);
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogDebug(ex, "WebSocket error during receive");
                break;
            }
        }
    }
}
