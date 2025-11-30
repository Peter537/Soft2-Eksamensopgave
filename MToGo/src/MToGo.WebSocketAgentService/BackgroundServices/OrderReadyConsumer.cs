using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.BackgroundServices;

// Kafka consumer: order-ready -> send to specific agent
// When partner marks food as ready, notify the assigned agent to come pick it up
public class OrderReadyConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentConnectionManager _connectionManager;
    private readonly ILogger<OrderReadyConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderReadyConsumer(
        IServiceProvider serviceProvider,
        AgentConnectionManager connectionManager,
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

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-agent-service-order-ready",
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
                    "Received OrderReadyEvent: OrderId={OrderId}, AgentId={AgentId}",
                    readyEvent.OrderId, readyEvent.AgentId);

                if (!readyEvent.AgentId.HasValue)
                {
                    _logger.LogWarning("OrderReadyEvent has no AgentId, cannot notify agent");
                    return;
                }

                // Send to the specific agent - their order is ready for pickup
                var sent = await _connectionManager.SendToAgentAsync(
                    readyEvent.AgentId.Value,
                    "OrderReady",
                    readyEvent);

                if (sent)
                {
                    _logger.LogInformation("OrderReady notification sent to Agent {AgentId}", readyEvent.AgentId);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId} not connected, OrderReady not delivered", readyEvent.AgentId);
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
