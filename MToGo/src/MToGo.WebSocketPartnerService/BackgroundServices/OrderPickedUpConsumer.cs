using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.BackgroundServices;

// Kafka consumer: order-pickedup -> remove order from partner's view
public class OrderPickedUpConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PartnerConnectionManager _connectionManager;
    private readonly ILogger<OrderPickedUpConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderPickedUpConsumer(
        IServiceProvider serviceProvider,
        PartnerConnectionManager connectionManager,
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

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-partner-service-order-pickedup",
            Topics = new List<string> { KafkaTopics.OrderPickedUp }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("OrderPickedUpConsumer listening on topic: {Topic}", KafkaTopics.OrderPickedUp);

        try
        {
            await consumer.ConsumeAsync<OrderPickedUpEvent>(async pickupEvent =>
            {
                _logger.LogInformation(
                    "Received OrderPickedUpEvent: OrderId={OrderId}, PartnerId={PartnerId}",
                    pickupEvent.OrderId, pickupEvent.PartnerId);

                var sent = await _connectionManager.SendToPartnerAsync(
                    pickupEvent.PartnerId,
                    "OrderPickedUp",
                    pickupEvent);

                if (sent)
                {
                    _logger.LogInformation("OrderPickedUp pushed to Partner {PartnerId}", pickupEvent.PartnerId);
                }
                else
                {
                    _logger.LogWarning("Partner {PartnerId} not connected, OrderPickedUp not delivered", pickupEvent.PartnerId);
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
