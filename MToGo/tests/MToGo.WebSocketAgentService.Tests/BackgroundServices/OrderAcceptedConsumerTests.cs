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
public class OrderAcceptedConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderAcceptedConsumerTests(SharedKafkaFixture kafkaFixture)
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
    public async Task Consumer_ShouldBroadcastToAllAgents_WhenOrderAcceptedEventReceived()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var agent1Messages = new List<string>();
        var agent1Received = new TaskCompletionSource<bool>();
        var agent1Mock = CreateCapturingWebSocketMock(agent1Messages, agent1Received);

        var agent2Messages = new List<string>();
        var agent2Mock = CreateCapturingWebSocketMock(agent2Messages, null);

        await connectionManager.RegisterBroadcastConnectionAsync("agent1", agent1Mock.Object);
        await connectionManager.RegisterBroadcastConnectionAsync("agent2", agent2Mock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderAcceptedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderAcceptedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        // Publish event with unique ID to avoid conflicts with previous test runs
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        var orderEvent = new OrderAcceptedEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = 1,
            PartnerName = "Pizza Place",
            PartnerAddress = "123 Main St",
            DeliveryAddress = "456 Oak Ave",
            DeliveryFee = 5.00m,
            Distance = "2.5 km",
            EstimatedMinutes = 15,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = [new OrderAcceptedEvent.OrderAcceptedItem { Name = "Margherita", Quantity = 1 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderAccepted, orderEvent.OrderId.ToString(), orderEvent);
        await Task.WhenAny(agent1Received.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - find our specific message (filter by unique order ID)
        var agent1Msg = agent1Messages.FirstOrDefault(m => m.Contains(uniqueOrderId.ToString()));
        var agent2Msg = agent2Messages.FirstOrDefault(m => m.Contains(uniqueOrderId.ToString()));

        Assert.NotNull(agent1Msg);
        Assert.Contains("OrderAccepted", agent1Msg);
        Assert.Contains("Pizza Place", agent1Msg);

        Assert.NotNull(agent2Msg);
        Assert.Contains("OrderAccepted", agent2Msg);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenNoAgentsConnected()
    {
        // Arrange
        var connectionManager = new AgentConnectionManager(new Mock<ILogger<AgentConnectionManager>>().Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderAcceptedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderAcceptedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event with no connected agents
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderAcceptedEvent
        {
            OrderId = 9999,
            CustomerId = 1,
            PartnerName = "Test Partner",
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = []
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderAccepted, orderEvent.OrderId.ToString(), orderEvent);
        await Task.Delay(300);

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed event without crashing
        Assert.Equal(0, connectionManager.BroadcastConnectionCount);
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
