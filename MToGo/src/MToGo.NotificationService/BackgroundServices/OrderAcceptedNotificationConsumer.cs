using MToGo.NotificationService.Models;
using MToGo.NotificationService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.NotificationService.BackgroundServices;

// Kafka consumer: order-accepted -> send notification to customer
// When a partner accepts an order, notify the customer
public class OrderAcceptedNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderAcceptedNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderAcceptedNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<OrderAcceptedNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderAcceptedNotificationConsumer starting...");

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId:OrderAccepted"] ?? "notification-service-order-accepted",
            Topics = new List<string> { KafkaTopics.OrderAccepted }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderAcceptedNotificationConsumer listening on topic: {Topic}", KafkaTopics.OrderAccepted);

        try
        {
            await consumer.ConsumeAsync<OrderAcceptedEvent>(async orderEvent =>
            {
                _logger.LogInformation(
                    "Received OrderAcceptedEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                    orderEvent.OrderId, orderEvent.CustomerId);

                await SendNotificationAsync(
                    orderEvent.CustomerId,
                    $"Your order #{orderEvent.OrderId} has been accepted by {orderEvent.PartnerName}. Estimated time: {orderEvent.EstimatedMinutes} minutes.");

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderAcceptedNotificationConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderAcceptedNotificationConsumer encountered an error");
        }
    }

    private async Task SendNotificationAsync(int customerId, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            await notificationService.SendNotificationAsync(new NotificationRequest
            {
                CustomerId = customerId,
                Message = message
            });
            _logger.LogInformation("Notification sent to customer {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to customer {CustomerId}", customerId);
        }
    }
}
