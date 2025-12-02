using System.Net.WebSockets;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.Handlers;

/// <summary>
/// Handles WebSocket connections for customers.
/// Endpoint: /customers/{id} - Customer receives order status updates
/// </summary>
public class CustomerWebSocketHandler
{
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<CustomerWebSocketHandler> _logger;

    public CustomerWebSocketHandler(
        CustomerConnectionManager connectionManager,
        ILogger<CustomerWebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Handle customer WebSocket connection.
    /// Customer will receive updates about all their orders (accepted, rejected, picked up, delivered).
    /// </summary>
    public async Task HandleConnectionAsync(HttpContext context, int customerId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
            return;
        }

        _logger.LogInformation("Customer {CustomerId} connecting for order updates", customerId);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await _connectionManager.RegisterConnectionAsync(customerId, webSocket);

        try
        {
            await KeepConnectionAliveAsync(webSocket);
        }
        finally
        {
            _connectionManager.RemoveConnection(customerId);
            _logger.LogInformation("Customer {CustomerId} disconnected", customerId);
        }
    }

    /// <summary>
    /// Keep WebSocket alive until client disconnects.
    /// </summary>
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
                    _logger.LogDebug("Received {Count} bytes from customer (ignored)", result.Count);
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
