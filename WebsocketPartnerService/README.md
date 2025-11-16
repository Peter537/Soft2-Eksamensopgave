# WebSocket Partner Service - Implementation Guide

## Purpose
This microservice manages real-time WebSocket connections between the backend and restaurant partner devices (iPads, tablets, etc.). It enables instant notifications when new orders arrive without the need for polling.

## Current Status: PLACEHOLDER IMPLEMENTATION ⚠️
The current implementation provides example code showing the intended architecture. SignalR integration needs to be completed by the development team.

## How It Should Work

### Architecture Overview
```
┌──────────────┐         WebSocket          ┌────────────────────────┐
│ Restaurant   │◄──────────────────────────►│ WebsocketPartnerSvc   │
│    UI        │         Connection          │                        │
│ (Frontend)   │                             │  SignalR Hub           │
└──────────────┘                             └───────────┬────────────┘
                                                         │
                                                         │ Consumes
                                                         ▼
                                             ┌────────────────────────┐
                                             │   Kafka Event Bus      │
                                             │                        │
                                             │  - OrderCreated        │
                                             │  - OrderAccepted       │
                                             │  - OrderRejected       │
                                             └────────────────────────┘
```

### Expected Flow

1. **Restaurant UI Opens**
   - Frontend connects to WebSocket endpoint: `ws://localhost:5003/partnerhub`
   - Connection is authenticated with restaurant/partner ID
   - Connection is stored in memory (or Redis for scaling)

2. **Customer Places Order**
   - OrderService saves order to DB
   - OrderService publishes `OrderCreated` event to Kafka
   - WebsocketPartnerService consumes the event
   - Service looks up which restaurant the order belongs to
   - Service pushes notification to that restaurant's WebSocket connection

3. **Real-time Update**
   - Restaurant UI receives WebSocket message with order details
   - UI displays new order notification/card
   - Restaurant can then accept or reject via HTTP calls to CentralHub

4. **Restaurant Accepts Order**
   - UI sends `POST /api/orders/{id}/accept` to CentralHub
   - CentralHub routes to OrderService
   - OrderService publishes `OrderAccepted` event
   - WebsocketPartnerService can notify the restaurant of confirmation (optional)

## Implementation TODO for Developers

### 1. Fix SignalR Hub Implementation
```csharp
// Current: Placeholder code
// Needed: Proper SignalR Hub with connection management

public class PartnerHub : Hub
{
    // Add connection ID mapping to restaurant/partner ID
    public override async Task OnConnectedAsync()
    {
        var restaurantId = Context.GetHttpContext()?.Request.Query["restaurantId"];
        // Store mapping: connectionId -> restaurantId
        await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant_{restaurantId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up connection mapping
        await base.OnDisconnectedAsync(exception);
    }
}
```

### 2. Kafka Consumer Integration
```csharp
// In OrderCreatedConsumer.cs
// When OrderCreated event is consumed:

var orderEvent = /* consume from Kafka */;
var restaurantId = orderEvent.RestaurantId;

// Push to restaurant's WebSocket
await _hubContext.Clients
    .Group($"restaurant_{restaurantId}")
    .SendAsync("NewOrder", new {
        orderId = orderEvent.OrderId,
        customerName = orderEvent.CustomerName,
        items = orderEvent.Items,
        totalPrice = orderEvent.TotalPrice
    });
```

### 3. Frontend WebSocket Client
```javascript
// In Restaurant UI (Blazor/JavaScript)
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`ws://localhost:5003/partnerhub?restaurantId=${restaurantId}`)
    .build();

connection.on("NewOrder", (order) => {
    console.log("New order received:", order);
    // Update UI with new order
    displayNewOrder(order);
});

await connection.start();
```

### 4. Testing Locally
1. Start Kafka: `docker-compose up kafka -d`
2. Run WebsocketPartnerService: `dotnet run`
3. Connect from frontend
4. Create test order and verify WebSocket message is received

### 5. Scaling Considerations
For production with multiple instances:
- Use Redis backplane for SignalR: `services.AddSignalR().AddStackExchangeRedis(...)`
- This allows messages to reach clients connected to different server instances

## Known Issues
- SignalR currently causes button handlers to break in Blazor
  - Investigate Blazor SignalR compatibility
  - May need to isolate SignalR in separate JavaScript context
  - Consider using native WebSocket API instead of SignalR client library

## Files to Review
- `Program.cs` - SignalR setup and Kafka configuration
- `BackgroundServices/OrderCreatedConsumer.cs` - Kafka → WebSocket bridge
- `Services/PartnerConnectionService.cs` - Connection management (create this)

## References
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr)
- [SignalR with Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/tutorials/signalr-blazor)
- [Kafka Consumer in .NET](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
