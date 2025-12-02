using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.BackgroundServices;

/// <summary>
/// Kafka consumer: order-rejected -> send to customer
/// When a partner rejects an order, notify the customer with the reason.
/// </summary>
public class OrderRejectedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<OrderRejectedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderRejectedConsumer(
        IServiceProvider serviceProvider,
        CustomerConnectionManager connectionManager,
        ILogger<OrderRejectedConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderRejectedConsumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-customer-service-order-rejected",
            Topics = new List<string> { KafkaTopics.OrderRejected }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderRejectedConsumer listening on topic: {Topic}", KafkaTopics.OrderRejected);

        try
        {
            await consumer.ConsumeAsync<OrderRejectedEvent>(async rejectedEvent =>
            {
                _logger.LogInformation(
                    "Received OrderRejectedEvent: OrderId={OrderId}, CustomerId={CustomerId}, Reason={Reason}",
                    rejectedEvent.OrderId, rejectedEvent.CustomerId, rejectedEvent.Reason);

                var payload = new
                {
                    orderId = rejectedEvent.OrderId,
                    reason = rejectedEvent.Reason,
                    timestamp = rejectedEvent.Timestamp
                };

                var sent = await _connectionManager.SendToCustomerAsync(
                    rejectedEvent.CustomerId,
                    "OrderRejected",
                    payload);

                if (sent)
                {
                    _logger.LogInformation("OrderRejected notification sent to Customer {CustomerId}", rejectedEvent.CustomerId);
                }
                else
                {
                    _logger.LogDebug("Customer {CustomerId} not connected, OrderRejected not delivered", rejectedEvent.CustomerId);
                }

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderRejectedConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderRejectedConsumer encountered an error");
        }
    }
}
