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
public class OrderPickedUpConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderPickedUpConsumerTests(SharedKafkaFixture kafkaFixture)
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
    public async Task Consumer_ShouldNotifyPartner_WithOrderIdAndAgentName_WhenOrderPickedUp()
    {
        // Arrange
        var connectionManager = new PartnerConnectionManager(new Mock<ILogger<PartnerConnectionManager>>().Object);

        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var webSocketMock = CreateCapturingWebSocketMock(receivedMessages, messageReceived);

        var partnerId = 77;
        await connectionManager.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderPickedUpConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderPickedUpConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        // Act
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var pickedUpEvent = new OrderPickedUpEvent
        {
            OrderId = 999,
            PartnerId = partnerId,
            CustomerId = 50,
            AgentName = "Delivery Dave",
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderPickedUp, pickedUpEvent.OrderId.ToString(), pickedUpEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - Partner receives notification with order ID and agent name
        Assert.Single(receivedMessages);
        var msg = receivedMessages[0];
        Assert.Contains("OrderPickedUp", msg);
        Assert.Contains("999", msg);
        Assert.Contains("Delivery Dave", msg);
    }

    [Fact]
    public async Task Consumer_ShouldOnlyNotifyTargetPartner_NotOthers()
    {
        // Arrange
        var connectionManager = new PartnerConnectionManager(new Mock<ILogger<PartnerConnectionManager>>().Object);

        // Two partners connected
        var partner1Messages = new List<string>();
        var partner2Messages = new List<string>();
        var signal = new TaskCompletionSource<bool>();

        var socket1 = CreateCapturingWebSocketMock(partner1Messages, signal);
        var socket2 = CreateCapturingWebSocketMock(partner2Messages, null);

        await connectionManager.RegisterConnectionAsync(1, socket1.Object);
        await connectionManager.RegisterConnectionAsync(2, socket2.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderPickedUpConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderPickedUpConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500); // Wait for consumer to be ready

        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        // Event targeting partner 1 only
        var pickedUpEvent = new OrderPickedUpEvent
        {
            OrderId = 555,
            PartnerId = 1,
            CustomerId = 10,
            AgentName = "Bob",
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderPickedUp, pickedUpEvent.OrderId.ToString(), pickedUpEvent);
        await Task.WhenAny(signal.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - only partner 1 should receive the message
        Assert.Single(partner1Messages);
        Assert.Empty(partner2Messages);
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
