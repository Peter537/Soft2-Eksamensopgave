using Shared.Kafka;
using Shared.Events;
using PartnerService.Services;
using PartnerService.Models;

namespace PartnerService.BackgroundServices;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly PartnerOrderRepository _orderRepository;
    private readonly IConfiguration _configuration;
    private KafkaConsumerService? _consumer;

    public OrderCreatedConsumer(PartnerOrderRepository orderRepository, IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
        
        Console.WriteLine($"   🔌 Attempting to connect to Kafka at {bootstrapServers}...");
        
        try
        {
            _consumer = new KafkaConsumerService(
                bootstrapServers, 
                "partner-service-group", 
                KafkaTopics.OrderCreated
            );

            Console.WriteLine($"   ✅ Successfully subscribed to Kafka topic: {KafkaTopics.OrderCreated}");
            Console.WriteLine($"   👂 Now listening for incoming orders...\n");

            await _consumer.ConsumeAsync<OrderCreatedEvent>(async (orderEvent) =>
            {
                Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
                Console.WriteLine("│  🍕 NEW ORDER RECEIVED AT RESTAURANT                    │");
                Console.WriteLine("└─────────────────────────────────────────────────────────┘");
                Console.WriteLine($"   Order ID: {orderEvent.OrderId}");
                Console.WriteLine($"   Customer: {orderEvent.CustomerName}");
                Console.WriteLine($"   Address: {orderEvent.DeliveryAddress}");
                Console.WriteLine($"   Items: {orderEvent.OrderDetails}");
                Console.WriteLine($"   Total: {orderEvent.TotalPrice:C}");

                // Save to partner's local database
                var order = new Order
                {
                    OrderId = orderEvent.OrderId,
                    CustomerName = orderEvent.CustomerName,
                    DeliveryAddress = orderEvent.DeliveryAddress,
                    OrderDetails = orderEvent.OrderDetails,
                    TotalPrice = orderEvent.TotalPrice,
                    Status = "Pending",
                    CreatedAt = orderEvent.CreatedAt
                };

                _orderRepository.AddOrder(order);
                
                Console.WriteLine($"   ✅ Order saved to restaurant database");
                Console.WriteLine($"   ⏳ Waiting for restaurant to accept/reject...\n");

                await Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ KAFKA ERROR: {ex.Message}");
            Console.WriteLine($"   Make sure Kafka is running on {bootstrapServers}");
            throw;
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
