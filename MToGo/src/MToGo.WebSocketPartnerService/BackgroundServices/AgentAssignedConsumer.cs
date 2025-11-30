using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.BackgroundServices;

// Kafka consumer: agent-assigned -> notify partner via WebSocket
public class AgentAssignedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PartnerConnectionManager _connectionManager;
    private readonly ILogger<AgentAssignedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public AgentAssignedConsumer(
        IServiceProvider serviceProvider,
        PartnerConnectionManager connectionManager,
        ILogger<AgentAssignedConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentAssignedConsumer starting...");

        // Wait a bit for Kafka to be ready on startup
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        var config = new KafkaConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "websocket-partner-service-agent-assigned",
            Topics = new List<string> { KafkaTopics.AgentAssigned }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("AgentAssignedConsumer listening on topic: {Topic}", KafkaTopics.AgentAssigned);

        try
        {
            await consumer.ConsumeAsync<AgentAssignedEvent>(async agentEvent =>
            {
                _logger.LogInformation(
                    "Received AgentAssignedEvent: OrderId={OrderId}, PartnerId={PartnerId}, AgentId={AgentId}",
                    agentEvent.OrderId, agentEvent.PartnerId, agentEvent.AgentId);

                // Push to the partner's WebSocket if connected
                var sent = await _connectionManager.SendToPartnerAsync(
                    agentEvent.PartnerId,
                    "AgentAssigned",
                    agentEvent);

                if (sent)
                {
                    _logger.LogInformation("AgentAssigned pushed to Partner {PartnerId}", agentEvent.PartnerId);
                }
                else
                {
                    _logger.LogWarning("Partner {PartnerId} not connected, AgentAssigned not delivered", agentEvent.PartnerId);
                }
            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AgentAssignedConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentAssignedConsumer encountered an error");
        }
    }
}
