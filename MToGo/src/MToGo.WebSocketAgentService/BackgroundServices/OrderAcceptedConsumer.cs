using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.BackgroundServices;

// Kafka consumer: order-accepted -> broadcast to all agents
// When a partner accepts an order, all agents see it as a new delivery opportunity
public class OrderAcceptedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentConnectionManager _connectionManager;
    private readonly ILogger<OrderAcceptedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderAcceptedConsumer(
        IServiceProvider serviceProvider,
        AgentConnectionManager connectionManager,
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

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-agent-service-order-accepted",
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
            await consumer.ConsumeAsync<OrderAcceptedEvent>(async orderEvent =>
            {
                _logger.LogInformation(
                    "Received OrderAcceptedEvent: OrderId={OrderId}, Partner={PartnerName}",
                    orderEvent.OrderId, orderEvent.PartnerName);

                // Broadcast to ALL connected agents - new job available!
                await _connectionManager.BroadcastToAllAgentsAsync("OrderAccepted", orderEvent);

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
