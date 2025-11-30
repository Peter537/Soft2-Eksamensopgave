using System.Net.WebSockets;

namespace MToGo.WebSocketPartnerService.Handlers;

// Handles WS lifecycle: accept, register, keep-alive until client disconnects
public class PartnerWebSocketHandler
{
    private readonly Services.PartnerConnectionManager _connectionManager;
    private readonly ILogger<PartnerWebSocketHandler> _logger;

    public PartnerWebSocketHandler(
        Services.PartnerConnectionManager connectionManager,
        ILogger<PartnerWebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(HttpContext context, int partnerId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
            return;
        }

        _logger.LogInformation("Partner {PartnerId} requesting WebSocket connection", partnerId);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        await _connectionManager.RegisterConnectionAsync(partnerId, webSocket);

        try
        {
            // Keep connection alive by listening for messages (or close)
            await KeepConnectionAliveAsync(webSocket, partnerId);
        }
        finally
        {
            _connectionManager.RemoveConnection(partnerId);
        }
    }

    // Read loop to detect client disconnect. Partners only receive, never send.
    private async Task KeepConnectionAliveAsync(WebSocket webSocket, int partnerId)
    {
        var buffer = new byte[1024];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Partner {PartnerId} initiated close", partnerId);
                    
                    if (webSocket.State == WebSocketState.CloseReceived)
                    {
                        await webSocket.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client closed connection",
                            CancellationToken.None);
                    }
                    break;
                }

                // Partners don't send data, but log if they do (for debugging)
                if (result.Count > 0)
                {
                    _logger.LogDebug("Received {Bytes} bytes from Partner {PartnerId} (ignored)", 
                        result.Count, partnerId);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error for Partner {PartnerId}", partnerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for Partner {PartnerId}", partnerId);
        }
    }
}
