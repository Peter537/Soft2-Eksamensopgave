using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.BackgroundServices;

// Kafka consumer: agent-assigned -> broadcast to all agents
// When an agent accepts a job, all other agents should see it disappear from their list
public class AgentAssignedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentConnectionManager _connectionManager;
    private readonly ILogger<AgentAssignedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public AgentAssignedConsumer(
        IServiceProvider serviceProvider,
        AgentConnectionManager connectionManager,
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
            GroupId = "websocket-agent-service-agent-assigned",
            Topics = new List<string> { KafkaTopics.AgentAssigned }
        };

        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var consumerLogger = loggerFactory.CreateLogger<KafkaConsumer>();

        var options = Microsoft.Extensions.Options.Options.Create(config);
        await using var consumer = new KafkaConsumer(options, consumerLogger);

        _logger.LogInformation("AgentAssignedConsumer listening on topic: {Topic}", KafkaTopics.AgentAssigned);

        try
        {
            await consumer.ConsumeAsync<AgentAssignedEvent>(async assignedEvent =>
            {
                _logger.LogInformation(
                    "Received AgentAssignedEvent: OrderId={OrderId}, AgentId={AgentId}",
                    assignedEvent.OrderId, assignedEvent.AgentId);

                // Broadcast to ALL agents - this job is taken, remove from available list
                await _connectionManager.BroadcastToAllAgentsAsync("AgentAssigned", new
                {
                    orderId = assignedEvent.OrderId,
                    agentId = assignedEvent.AgentId
                });

                // Send full order details to the specific agent's personal room
                var deliveryAcceptedPayload = new
                {
                    orderId = assignedEvent.OrderId,
                    partnerName = assignedEvent.PartnerName,
                    partnerAddress = assignedEvent.PartnerAddress,
                    deliveryAddress = assignedEvent.DeliveryAddress,
                    deliveryFee = assignedEvent.DeliveryFee,
                    items = assignedEvent.Items.Select(i => new { name = i.Name, quantity = i.Quantity }).ToList()
                };

                var sent = await _connectionManager.SendToAgentAsync(
                    assignedEvent.AgentId, 
                    "DeliveryAccepted", 
                    deliveryAcceptedPayload);

                if (sent)
                {
                    _logger.LogInformation("Sent DeliveryAccepted to Agent {AgentId}", assignedEvent.AgentId);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId} not connected to personal room", assignedEvent.AgentId);
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
