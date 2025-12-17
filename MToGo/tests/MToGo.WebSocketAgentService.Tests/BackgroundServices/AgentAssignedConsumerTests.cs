using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketAgentService.BackgroundServices;
using MToGo.WebSocketAgentService.Services;
using MToGo.WebSocketAgentService.Tests.Fixtures;

namespace MToGo.WebSocketAgentService.Tests.BackgroundServices;

[Collection("Kafka")]
public class AgentAssignedConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public AgentAssignedConsumerTests(SharedKafkaFixture kafkaFixture)
    {
        _kafkaFixture = kafkaFixture;
    }

    private IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = _kafkaFixture.BootstrapServers
            })
            .Build();
    }

    [Fact]
    public async Task Consumer_ShouldBroadcastAgentAssigned_ToAllAgents()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var broadcastMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var broadcastMock = CreateCapturingWebSocketMock(broadcastMessages, messageReceived);

        await connectionManager.RegisterBroadcastConnectionAsync("broadcast-agent", broadcastMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new AgentAssignedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<AgentAssignedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var agentEvent = new AgentAssignedEvent
        {
            OrderId = 500,
            PartnerId = 10,
            AgentId = 42,
            PartnerName = "Test Restaurant",
            PartnerAddress = "123 Partner St",
            DeliveryAddress = "456 Customer Ave",
            DeliveryFee = 4.50m,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = [new AgentAssignedEvent.AgentAssignedItem { Name = "Burger", Quantity = 2 }]
        };

        await producer.PublishAsync(KafkaTopics.AgentAssigned, agentEvent.OrderId.ToString(), agentEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - broadcast message should contain order ID and agent ID
        Assert.Single(broadcastMessages);
        var msg = broadcastMessages[0];
        Assert.Contains("AgentAssigned", msg);
        Assert.Contains("500", msg);
        Assert.Contains("42", msg);
    }

    [Fact]
    public async Task Consumer_ShouldSendDeliveryAccepted_ToSpecificAgent()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var agentMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var agentMock = CreateCapturingWebSocketMock(agentMessages, messageReceived);

        var agentId = 42;
        await connectionManager.RegisterAgentConnectionAsync(agentId, agentMock.Object);

        // Also add a broadcast connection to ensure both paths work
        var broadcastMessages = new List<string>();
        var broadcastMock = CreateCapturingWebSocketMock(broadcastMessages, null);
        await connectionManager.RegisterBroadcastConnectionAsync("other-agent", broadcastMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new AgentAssignedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<AgentAssignedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var agentEvent = new AgentAssignedEvent
        {
            OrderId = 600,
            PartnerId = 10,
            AgentId = agentId,
            PartnerName = "Pizza Palace",
            PartnerAddress = "100 Pizza Lane",
            DeliveryAddress = "200 Customer Blvd",
            DeliveryFee = 6.00m,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = [new AgentAssignedEvent.AgentAssignedItem { Name = "Pepperoni Pizza", Quantity = 1 }]
        };

        await producer.PublishAsync(KafkaTopics.AgentAssigned, agentEvent.OrderId.ToString(), agentEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - agent should receive DeliveryAccepted with full order details
        Assert.Single(agentMessages);
        var msg = agentMessages[0];
        Assert.Contains("DeliveryAccepted", msg);
        Assert.Contains("600", msg);
        Assert.Contains("Pizza Palace", msg);
        Assert.Contains("100 Pizza Lane", msg);
        Assert.Contains("200 Customer Blvd", msg);
        Assert.Contains("Pepperoni Pizza", msg);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenAssignedAgentNotConnected()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        // Register a DIFFERENT agent to broadcast (not the assigned one)
        var broadcastMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var broadcastMock = CreateCapturingWebSocketMock(broadcastMessages, messageReceived);
        await connectionManager.RegisterBroadcastConnectionAsync("other-agent", broadcastMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new AgentAssignedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<AgentAssignedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event for agent that is NOT connected to personal room
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var agentEvent = new AgentAssignedEvent
        {
            OrderId = 700,
            PartnerId = 10,
            AgentId = 999, // This agent is NOT connected
            PartnerName = "Test",
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = []
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.AgentAssigned, agentEvent.OrderId.ToString(), agentEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - broadcast should still have been sent
        Assert.Single(broadcastMessages);
        Assert.Contains("AgentAssigned", broadcastMessages[0]);
    }

    private static Mock<WebSocket> CreateCapturingWebSocketMock(List<string> capturedMessages, TaskCompletionSource<bool>? signal = null)
    {
        var mock = new Mock<WebSocket>();
        mock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        mock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()))
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>((data, type, endOfMessage, ct) =>
            {
                capturedMessages.Add(Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count));
                signal?.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);
        mock.Setup(ws => ws.CloseAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }
}

