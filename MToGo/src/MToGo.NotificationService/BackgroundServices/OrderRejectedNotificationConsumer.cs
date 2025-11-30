using MToGo.NotificationService.Models;
using MToGo.NotificationService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.NotificationService.BackgroundServices;

// Kafka consumer: order-rejected -> send notification to customer
// When a partner rejects an order, notify the customer
public class OrderRejectedNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderRejectedNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderRejectedNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<OrderRejectedNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderRejectedNotificationConsumer starting...");

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId:OrderRejected"] ?? "notification-service-order-rejected",
            Topics = new List<string> { KafkaTopics.OrderRejected }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderRejectedNotificationConsumer listening on topic: {Topic}", KafkaTopics.OrderRejected);

        try
        {
            await consumer.ConsumeAsync<OrderRejectedEvent>(async orderEvent =>
            {
                _logger.LogInformation(
                    "Received OrderRejectedEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                    orderEvent.OrderId, orderEvent.CustomerId);

                var reasonText = string.IsNullOrEmpty(orderEvent.Reason)
                    ? ""
                    : $" Reason: {orderEvent.Reason}";

                await SendNotificationAsync(
                    orderEvent.CustomerId,
                    $"Your order #{orderEvent.OrderId} has been rejected.{reasonText}");

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderRejectedNotificationConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderRejectedNotificationConsumer encountered an error");
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
