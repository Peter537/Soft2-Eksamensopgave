using Shared.Events;
using Shared.Kafka;

namespace LocationService.BackgroundServices;

public class OrderPickedUpConsumer : BackgroundService
{
    private readonly ILogger<OrderPickedUpConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, CancellationTokenSource> _activeTracking = new();

    public OrderPickedUpConsumer(
        ILogger<OrderPickedUpConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var consumerService = new KafkaConsumerService(
            bootstrapServers,
            "location-service-group",
            KafkaTopics.OrderPickedUp
        );

        await consumerService.ConsumeAsync<OrderPickedUpEvent>(async (pickupEvent) =>
        {
            _logger.LogInformation($"🚚 Order {pickupEvent.OrderId} picked up by driver {pickupEvent.DriverId}");
            _logger.LogInformation($"📍 Starting GPS tracking simulation...");

            // Start tracking this order in a separate task
            var trackingCts = new CancellationTokenSource();
            _activeTracking[pickupEvent.OrderId] = trackingCts;

            // Run GPS simulation in background
            _ = Task.Run(async () =>
            {
                await SimulateGPSTracking(
                    pickupEvent.OrderId,
                    pickupEvent.DriverId,
                    bootstrapServers,
                    trackingCts.Token
                );
            }, trackingCts.Token);
        });
    }

    private async Task SimulateGPSTracking(
        string orderId,
        string driverId,
        string bootstrapServers,
        CancellationToken cancellationToken)
    {
        var kafkaProducer = new KafkaProducerService(bootstrapServers);
        
        // Starting coordinates (restaurant)
        double latitude = 55.6761;  // Copenhagen coordinates
        double longitude = 12.5683;
        
        // Destination coordinates (customer)
        double destLatitude = 55.6867;
        double destLongitude = 12.5700;
        
        int updateCount = 0;
        const int maxUpdates = 12; // 12 updates × 1 second = 12 seconds total
        bool arrivedNotificationSent = false;

        try
        {
            while (updateCount < maxUpdates && !cancellationToken.IsCancellationRequested)
            {
                updateCount++;
                
                // Move towards destination using linear progression
                double progressRatio = (double)updateCount / maxUpdates;
                latitude = 55.6761 + (destLatitude - 55.6761) * progressRatio;
                longitude = 12.5683 + (destLongitude - 12.5683) * progressRatio;
                
                // Calculate progress percentage
                int progress = (updateCount * 100) / maxUpdates;

                // Publish location update
                await kafkaProducer.PublishAsync(
                    KafkaTopics.LocationUpdate,
                    orderId,
                    new LocationUpdateEvent
                    {
                        OrderId = orderId,
                        DriverId = driverId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = DateTime.UtcNow
                    }
                );

                // Beautiful console output
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n📍 GPS Update #{updateCount}/{maxUpdates} for Order: {orderId}");
                Console.WriteLine($"├─ Driver: {driverId}");
                Console.WriteLine($"├─ Location: {latitude:F4}°N, {longitude:F4}°E");
                Console.WriteLine($"├─ Progress: {progress}%");
                Console.WriteLine($"└─ Time: {DateTime.UtcNow:HH:mm:ss}");
                Console.ResetColor();

                // Send "driver arriving" notification at 80% progress (update 10 of 12)
                if (updateCount == 10 && !arrivedNotificationSent)
                {
                    await kafkaProducer.PublishAsync(
                        KafkaTopics.DriverArriving,
                        orderId,
                        new DriverArrivingEvent
                        {
                            OrderId = orderId,
                            DriverId = driverId,
                            Latitude = latitude,
                            Longitude = longitude,
                            EstimatedMinutes = 1,
                            Timestamp = DateTime.UtcNow
                        }
                    );

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n⏰ DRIVER ARRIVING SOON! (80% complete)");
                    Console.WriteLine($"└─ Notification sent for order {orderId}");
                    Console.ResetColor();
                    
                    arrivedNotificationSent = true;
                }

                // Wait 1 second before next update (12 × 1s = 12 seconds total)
                await Task.Delay(1000, cancellationToken);
            }

            // Final update - arrived at destination, publish delivered event
            await kafkaProducer.PublishAsync(
                KafkaTopics.OrderDelivered,
                orderId,
                new OrderDeliveredEvent
                {
                    OrderId = orderId,
                    PhotoUrl = "https://example.com/delivery-photo.jpg",
                    DeliveredAt = DateTime.UtcNow
                }
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n🎯 Driver arrived at destination!");
            Console.WriteLine($"├─ Order {orderId} delivered successfully");
            Console.WriteLine($"└─ Delivery confirmation sent");
            Console.ResetColor();

            // Clean up
            _activeTracking.Remove(orderId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"GPS tracking stopped for order {orderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GPS tracking for order {orderId}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cancel all active tracking when service stops
        foreach (var cts in _activeTracking.Values)
        {
            cts.Cancel();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
