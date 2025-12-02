using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.BackgroundServices;

/// <summary>
/// Kafka consumer: order-accepted -> send to customer
/// When a partner accepts an order, notify the customer.
/// </summary>
public class OrderAcceptedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<OrderAcceptedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderAcceptedConsumer(
        IServiceProvider serviceProvider,
        CustomerConnectionManager connectionManager,
        ILogger<OrderAcceptedConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderAcceptedConsumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-customer-service-order-accepted",
            Topics = new List<string> { KafkaTopics.OrderAccepted }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderAcceptedConsumer listening on topic: {Topic}", KafkaTopics.OrderAccepted);

        try
        {
            await consumer.ConsumeAsync<OrderAcceptedEvent>(async acceptedEvent =>
            {
                _logger.LogInformation(
                    "Received OrderAcceptedEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                    acceptedEvent.OrderId, acceptedEvent.CustomerId);

                var payload = new
                {
                    orderId = acceptedEvent.OrderId,
                    partnerName = acceptedEvent.PartnerName,
                    estimatedMinutes = acceptedEvent.EstimatedMinutes,
                    timestamp = acceptedEvent.Timestamp
                };

                var sent = await _connectionManager.SendToCustomerAsync(
                    acceptedEvent.CustomerId,
                    "OrderAccepted",
                    payload);

                if (sent)
                {
                    _logger.LogInformation("OrderAccepted notification sent to Customer {CustomerId}", acceptedEvent.CustomerId);
                }
                else
                {
                    _logger.LogDebug("Customer {CustomerId} not connected, OrderAccepted not delivered", acceptedEvent.CustomerId);
                }

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderAcceptedConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderAcceptedConsumer encountered an error");
        }
    }
}
