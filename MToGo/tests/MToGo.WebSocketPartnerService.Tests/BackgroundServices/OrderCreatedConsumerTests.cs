using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketPartnerService.BackgroundServices;
using MToGo.WebSocketPartnerService.Services;
using MToGo.WebSocketPartnerService.Tests.Fixtures;

namespace MToGo.WebSocketPartnerService.Tests.BackgroundServices;

[Collection("Kafka")]
public class OrderCreatedConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderCreatedConsumerTests(SharedKafkaFixture kafkaFixture)
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
    public async Task Consumer_ShouldSendToPartner_WhenOrderCreatedEventReceived()
    {
        // Arrange
        var connectionManager = new PartnerConnectionManager(new Mock<ILogger<PartnerConnectionManager>>().Object);

        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var webSocketMock = CreateCapturingWebSocketMock(receivedMessages, messageReceived);

        var partnerId = 5;
        await connectionManager.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderCreatedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderCreatedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 1001,
            PartnerId = partnerId,
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = [new OrderCreatedEvent.OrderCreatedItem { Name = "Burger", Quantity = 2 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Single(receivedMessages);
        Assert.Contains("OrderCreated", receivedMessages[0]);
        Assert.Contains("1001", receivedMessages[0]);
        Assert.Contains("Burger", receivedMessages[0]);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenPartnerNotConnected()
    {
        // Arrange
        var connectionManager = new PartnerConnectionManager(new Mock<ILogger<PartnerConnectionManager>>().Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderCreatedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderCreatedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event for partner that's NOT connected
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 9991,
            PartnerId = 9991, // Not connected
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = []
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.Delay(300);

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed event without crashing
        Assert.Equal(0, connectionManager.ConnectionCount);
    }

    [Fact]
    public async Task Consumer_ShouldRouteToCorrectPartner_WhenMultipleConnected()
    {
        // Arrange
        var connectionManager = new PartnerConnectionManager(new Mock<ILogger<PartnerConnectionManager>>().Object);

        var partner1Messages = new List<string>();
        var partner1Mock = CreateCapturingWebSocketMock(partner1Messages, null);

        var partner2Messages = new List<string>();
        var partner2Received = new TaskCompletionSource<bool>();
        var partner2Mock = CreateCapturingWebSocketMock(partner2Messages, partner2Received);

        await connectionManager.RegisterConnectionAsync(101, partner1Mock.Object);
        await connectionManager.RegisterConnectionAsync(102, partner2Mock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderCreatedConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderCreatedConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event for Partner 102 only
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 12345,
            PartnerId = 102,
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = [new OrderCreatedEvent.OrderCreatedItem { Name = "Pizza", Quantity = 1 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.WhenAny(partner2Received.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Empty(partner1Messages);
        Assert.Single(partner2Messages);
        Assert.Contains("Pizza", partner2Messages[0]);
    }

    private static Mock<WebSocket> CreateCapturingWebSocketMock(List<string> capturedMessages, TaskCompletionSource<bool>? signal)
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
        return mock;
    }
}
