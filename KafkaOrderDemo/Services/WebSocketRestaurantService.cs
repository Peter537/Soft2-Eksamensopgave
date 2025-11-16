using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace KafkaOrderDemo.Services;

/// <summary>
/// PLACEHOLDER/EXAMPLE WebSocket Service for connecting to WebsocketPartnerService.
/// 
/// This demonstrates how restaurants would connect via WebSocket to receive
/// real-time notifications when new orders arrive.
/// 
/// TODO for C# developers:
/// 1. Implement proper connection management and reconnection logic
/// 2. Add authentication/authorization
/// 3. Handle WebSocket lifecycle events properly
/// 4. Integrate with Blazor UI state management
/// 5. Consider using SignalR instead of raw WebSockets for easier implementation
/// 
/// Current status: EXAMPLE CODE - Not hooked up to UI
/// </summary>
public class WebSocketRestaurantService : IDisposable
{
    private ClientWebSocket? _webSocket;
    private readonly string _websocketPartnerServiceUrl;
    private CancellationTokenSource? _cts;
    
    // Event to notify UI when a new order arrives via WebSocket
    public event Action<OrderNotification>? OnNewOrderReceived;

    public WebSocketRestaurantService(IConfiguration configuration)
    {
        // WebSocket endpoint (note: ws:// not http://)
        _websocketPartnerServiceUrl = configuration["ServiceUrls:WebsocketPartnerService"] 
            ?? "ws://localhost:5250/ws?restaurantId=RESTAURANT-001";
    }

    /// <summary>
    /// Connect to WebSocket server.
    /// In production, call this on app startup or when restaurant logs in.
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            Console.WriteLine($"🔌 Connecting to WebSocket: {_websocketPartnerServiceUrl}");
            
            await _webSocket.ConnectAsync(new Uri(_websocketPartnerServiceUrl), _cts.Token);
            
            Console.WriteLine("✅ WebSocket connected! Restaurant will receive real-time notifications.");

            // Start listening for messages
            _ = Task.Run(() => ReceiveMessagesAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket connection failed: {ex.Message}");
            Console.WriteLine("   (This is expected if WebsocketPartnerService is not running)");
        }
    }

    /// <summary>
    /// Listen for incoming WebSocket messages.
    /// When OrderCreated event arrives, trigger UI update.
    /// </summary>
    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<WebSocketMessage>(json);

                    if (message?.Type == "new_order")
                    {
                        Console.WriteLine($"🔔 WebSocket: New order received!");
                        
                        // Parse order data and notify UI
                        var orderData = JsonSerializer.Deserialize<OrderNotification>(
                            JsonSerializer.Serialize(message.Data)
                        );

                        if (orderData != null)
                        {
                            OnNewOrderReceived?.Invoke(orderData);
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
        _cts?.Dispose();
    }

    // WebSocket message structure
    private class WebSocketMessage
    {
        public string Type { get; set; } = "";
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Order notification from WebSocket
    public class OrderNotification
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public string OrderDetails { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

// ============================================================================
// HOW TO USE THIS SERVICE (for C# developers):
// ============================================================================
// 
// 1. Register in Program.cs:
//    builder.Services.AddSingleton<WebSocketRestaurantService>();
//
// 2. Inject into Kitchen.razor:
//    @inject WebSocketRestaurantService WebSocketService
//
// 3. Connect on component initialization:
//    protected override async Task OnInitializedAsync()
//    {
//        WebSocketService.OnNewOrderReceived += HandleNewOrder;
//        await WebSocketService.ConnectAsync();
//    }
//
// 4. Handle incoming orders:
//    private void HandleNewOrder(WebSocketRestaurantService.OrderNotification order)
//    {
//        // Show popup, play sound, refresh order list, etc.
//        Console.WriteLine($"New order via WebSocket: {order.OrderId}");
//        await LoadOrders(); // Refresh order list
//        StateHasChanged(); // Update UI
//    }
//
// 5. Clean up:
//    public void Dispose()
//    {
//        WebSocketService.OnNewOrderReceived -= HandleNewOrder;
//        WebSocketService.Dispose();
//    }
// ============================================================================
