using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.BackgroundServices;

/// <summary>
/// Kafka consumer: order-delivered -> send to customer
/// When an agent completes the delivery, notify the customer.
/// </summary>
public class OrderDeliveredConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<OrderDeliveredConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderDeliveredConsumer(
        IServiceProvider serviceProvider,
        CustomerConnectionManager connectionManager,
        ILogger<OrderDeliveredConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderDeliveredConsumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-customer-service-order-delivered",
            Topics = new List<string> { KafkaTopics.OrderDelivered }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderDeliveredConsumer listening on topic: {Topic}", KafkaTopics.OrderDelivered);

        try
        {
            await consumer.ConsumeAsync<OrderDeliveredEvent>(async deliveredEvent =>
            {
                _logger.LogInformation(
                    "Received OrderDeliveredEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                    deliveredEvent.OrderId, deliveredEvent.CustomerId);

                var payload = new
                {
                    orderId = deliveredEvent.OrderId,
                    timestamp = deliveredEvent.Timestamp
                };

                var sent = await _connectionManager.SendToCustomerAsync(
                    deliveredEvent.CustomerId,
                    "OrderDelivered",
                    payload);

                if (sent)
                {
                    _logger.LogInformation("OrderDelivered notification sent to Customer {CustomerId}", deliveredEvent.CustomerId);
                }
                else
                {
                    _logger.LogDebug("Customer {CustomerId} not connected, OrderDelivered not delivered", deliveredEvent.CustomerId);
                }

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderDeliveredConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderDeliveredConsumer encountered an error");
        }
    }
}
