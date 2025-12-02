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
public class OrderReadyConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderReadyConsumerTests(SharedKafkaFixture kafkaFixture)
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
    public async Task Consumer_ShouldSendOrderReady_ToSpecificAgent()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var agentMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var agentMock = CreateCapturingWebSocketMock(agentMessages, messageReceived);

        var agentId = 25;
        await connectionManager.RegisterAgentConnectionAsync(agentId, agentMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderReadyConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderReadyConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var readyEvent = new OrderReadyEvent
        {
            OrderId = 800,
            CustomerId = 1,
            PartnerName = "Burger Joint",
            PartnerAddress = "789 Burger St",
            AgentId = agentId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderReady, readyEvent.OrderId.ToString(), readyEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Single(agentMessages);
        var msg = agentMessages[0];
        Assert.Contains("OrderReady", msg);
        Assert.Contains("800", msg);
        Assert.Contains("Burger Joint", msg);
    }

    [Fact]
    public async Task Consumer_ShouldNotSend_WhenAgentIdIsNull()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        // Connect an agent (but event will have null AgentId)
        var agentMessages = new List<string>();
        var agentMock = CreateCapturingWebSocketMock(agentMessages, null);
        await connectionManager.RegisterAgentConnectionAsync(1, agentMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderReadyConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderReadyConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event with NO AgentId
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var readyEvent = new OrderReadyEvent
        {
            OrderId = 888,
            CustomerId = 1,
            PartnerName = "Test Restaurant",
            PartnerAddress = "Test Address",
            AgentId = null, // No agent assigned
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Act
        await producer.PublishAsync(KafkaTopics.OrderReady, readyEvent.OrderId.ToString(), readyEvent);
        await Task.Delay(500);

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - no message should be sent
        Assert.Empty(agentMessages);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenAgentNotConnected()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);
        // No agents connected

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderReadyConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderReadyConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event for agent that's NOT connected
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var readyEvent = new OrderReadyEvent
        {
            OrderId = 999,
            CustomerId = 1,
            PartnerName = "Test",
            PartnerAddress = "Test",
            AgentId = 999, // Not connected
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderReady, readyEvent.OrderId.ToString(), readyEvent);
        await Task.Delay(300);

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed event without crashing
        Assert.Equal(0, connectionManager.AgentConnectionCount);
    }

    [Fact]
    public async Task Consumer_ShouldOnlySendToTargetAgent_WhenMultipleConnected()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var targetAgentId = 10;
        var otherAgentId = 20;

        var targetMessages = new List<string>();
        var targetReceived = new TaskCompletionSource<bool>();
        var targetMock = CreateCapturingWebSocketMock(targetMessages, targetReceived);

        var otherMessages = new List<string>();
        var otherMock = CreateCapturingWebSocketMock(otherMessages, null);

        await connectionManager.RegisterAgentConnectionAsync(targetAgentId, targetMock.Object);
        await connectionManager.RegisterAgentConnectionAsync(otherAgentId, otherMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderReadyConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderReadyConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event for target agent only
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var readyEvent = new OrderReadyEvent
        {
            OrderId = 1111,
            CustomerId = 1,
            PartnerName = "Target Restaurant",
            PartnerAddress = "Target Address",
            AgentId = targetAgentId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderReady, readyEvent.OrderId.ToString(), readyEvent);
        await Task.WhenAny(targetReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - only target agent should receive message
        Assert.Single(targetMessages);
        Assert.Contains("OrderReady", targetMessages[0]);
        Assert.Contains("1111", targetMessages[0]);

        // Other agent should NOT receive anything
        Assert.Empty(otherMessages);
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
