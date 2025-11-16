using Confluent.Kafka;
using Shared.Events;
using Shared.Kafka;
using System.Text.Json;
using WebsocketPartnerService.Services;

namespace WebsocketPartnerService.BackgroundServices;

/// <summary>
/// Kafka consumer that listens for OrderCreated events and pushes them
/// to restaurant screens via WebSocket.
/// 
/// This demonstrates the event-driven architecture where:
/// 1. Customer creates order -> OrderService saves to DB
/// 2. OrderService publishes OrderCreated event to Kafka
/// 3. This consumer receives the event
/// 4. WebSocket pushes notification to restaurant's screen
/// </summary>
public class OrderCreatedWebSocketConsumer : BackgroundService
{
    private readonly WebSocketConnectionManager _webSocketManager;
    private readonly string _kafkaBootstrapServers;

    public OrderCreatedWebSocketConsumer(
        WebSocketConnectionManager webSocketManager,
        string kafkaBootstrapServers)
    {
        _webSocketManager = webSocketManager;
        _kafkaBootstrapServers = kafkaBootstrapServers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "websocket-partner-service-group",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.OrderCreated);

        Console.WriteLine($"🎧 Listening for {KafkaTopics.OrderCreated} events...\n");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);

                if (orderEvent != null)
                {
                    Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
                    Console.WriteLine("│  🔔 NEW ORDER EVENT - Pushing to Restaurant Screens     │");
                    Console.WriteLine("└─────────────────────────────────────────────────────────┘");
                    Console.WriteLine($"   Order ID: {orderEvent.OrderId}");
                    Console.WriteLine($"   Customer: {orderEvent.CustomerName}");
                    Console.WriteLine($"   Items: {orderEvent.OrderDetails}");
                    Console.WriteLine($"   Total: {orderEvent.TotalPrice:C}");

                    // Create WebSocket message for restaurant screens
                    var wsMessage = new
                    {
                        type = "new_order",
                        data = new
                        {
                            orderId = orderEvent.OrderId,
                            customerName = orderEvent.CustomerName,
                            deliveryAddress = orderEvent.DeliveryAddress,
                            orderDetails = orderEvent.OrderDetails,
                            totalPrice = orderEvent.TotalPrice,
                            createdAt = orderEvent.CreatedAt
                        },
                        timestamp = DateTime.UtcNow
                    };

                    // Broadcast to all connected restaurant screens
                    await _webSocketManager.BroadcastToAllRestaurants(wsMessage);

                    Console.WriteLine($"   ✅ WebSocket notification sent to restaurant screen(s)");
                    Console.WriteLine($"   💡 Restaurant can now Accept/Reject via CentralHub API\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in OrderCreated consumer: {ex.Message}");
        }
        finally
        {
            consumer.Close();
        }
    }
}
