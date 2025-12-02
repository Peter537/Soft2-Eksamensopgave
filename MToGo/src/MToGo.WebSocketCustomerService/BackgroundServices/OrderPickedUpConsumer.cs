using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.BackgroundServices;

/// <summary>
/// Kafka consumer: order-pickedup -> send to customer
/// When an agent picks up the order from the partner, notify the customer.
/// </summary>
public class OrderPickedUpConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly ILogger<OrderPickedUpConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderPickedUpConsumer(
        IServiceProvider serviceProvider,
        CustomerConnectionManager connectionManager,
        ILogger<OrderPickedUpConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderPickedUpConsumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-customer-service-order-pickedup",
            Topics = new List<string> { KafkaTopics.OrderPickedUp }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderPickedUpConsumer listening on topic: {Topic}", KafkaTopics.OrderPickedUp);

        try
        {
            await consumer.ConsumeAsync<OrderPickedUpEvent>(async pickedUpEvent =>
            {
                _logger.LogInformation(
                    "Received OrderPickedUpEvent: OrderId={OrderId}, CustomerId={CustomerId}, AgentName={AgentName}",
                    pickedUpEvent.OrderId, pickedUpEvent.CustomerId, pickedUpEvent.AgentName);

                var payload = new
                {
                    orderId = pickedUpEvent.OrderId,
                    agentName = pickedUpEvent.AgentName,
                    timestamp = pickedUpEvent.Timestamp
                };

                var sent = await _connectionManager.SendToCustomerAsync(
                    pickedUpEvent.CustomerId,
                    "OrderPickedUp",
                    payload);

                if (sent)
                {
                    _logger.LogInformation("OrderPickedUp notification sent to Customer {CustomerId}", pickedUpEvent.CustomerId);
                }
                else
                {
                    _logger.LogDebug("Customer {CustomerId} not connected, OrderPickedUp not delivered", pickedUpEvent.CustomerId);
                }

            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderPickedUpConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderPickedUpConsumer encountered an error");
        }
    }
}
