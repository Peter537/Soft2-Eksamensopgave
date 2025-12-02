using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.BackgroundServices;

/// <summary>
/// Kafka consumer: order-ready -> send to customer
/// When a partner marks the order as ready for pickup, notify the customer.
/// </summary>
public class OrderReadyConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<OrderReadyConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderReadyConsumer(
        IServiceProvider serviceProvider,
        CustomerConnectionManager connectionManager,
        ILogger<OrderReadyConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderReadyConsumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-customer-service-order-ready",
            Topics = new List<string> { KafkaTopics.OrderReady }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderReadyConsumer listening on topic: {Topic}", KafkaTopics.OrderReady);

        try
        {
            await consumer.ConsumeAsync<OrderReadyEvent>(async readyEvent =>
            {
                _logger.LogInformation(
                    "Received OrderReadyEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                    readyEvent.OrderId, readyEvent.CustomerId);

                var payload = new
                {
                    orderId = readyEvent.OrderId,
                    partnerName = readyEvent.PartnerName,
                    timestamp = readyEvent.Timestamp
                };

                var sent = await _connectionManager.SendToCustomerAsync(
                    readyEvent.CustomerId,
                    "OrderReady",
                    payload);

                if (sent)
                {
                    _logger.LogInformation("OrderReady notification sent to Customer {CustomerId}", readyEvent.CustomerId);
                }
                else
                {
                    _logger.LogDebug("Customer {CustomerId} not connected, OrderReady not delivered", readyEvent.CustomerId);
                }

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderReadyConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderReadyConsumer encountered an error");
        }
    }
}
