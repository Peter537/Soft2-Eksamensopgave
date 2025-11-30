using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.BackgroundServices;

// Kafka consumer: order-created -> push to partner's WebSocket
public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PartnerConnectionManager _connectionManager;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderCreatedConsumer(
        IServiceProvider serviceProvider,
        PartnerConnectionManager connectionManager,
        ILogger<OrderCreatedConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderCreatedConsumer starting...");

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-partner-service-order-created",
            Topics = new List<string> { KafkaTopics.OrderCreated }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderCreatedConsumer listening on topic: {Topic}", KafkaTopics.OrderCreated);

        try
        {
            await consumer.ConsumeAsync<OrderCreatedEvent>(async orderEvent =>
            {
                _logger.LogInformation(
                    "Received OrderCreatedEvent: OrderId={OrderId}, PartnerId={PartnerId}",
                    orderEvent.OrderId, orderEvent.PartnerId);

                // Push to the partner's WebSocket if connected
                var sent = await _connectionManager.SendToPartnerAsync(
                    orderEvent.PartnerId,
                    "OrderCreated",
                    orderEvent);

                if (sent)
                {
                    _logger.LogInformation("OrderCreated pushed to Partner {PartnerId}", orderEvent.PartnerId);
                }
                else
                {
                    _logger.LogWarning("Partner {PartnerId} not connected, OrderCreated not delivered", orderEvent.PartnerId);
                }
            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderCreatedConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderCreatedConsumer encountered an error");
        }
    }
}
