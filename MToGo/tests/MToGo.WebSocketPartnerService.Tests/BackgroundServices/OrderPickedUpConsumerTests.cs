using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.Testing;
using MToGo.WebSocketPartnerService.BackgroundServices;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.Tests.BackgroundServices;

public class OrderPickedUpConsumerTests
{
    [Fact]
    public async Task Consumer_ShouldNotifyPartner_WithOrderIdAndAgentName_WhenOrderPickedUp()
    {
        // Arrange - Tests full flow: agent picks up order, partner gets notified with details
        await using var kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        await kafkaContainer.StartAsync();
        var bootstrapServers = kafkaContainer.GetBootstrapAddress();

        var connectionManager = new PartnerConnectionManager(
            new Mock<ILogger<PartnerConnectionManager>>().Object);

        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var webSocketMock = CreateCapturingWebSocketMock(receivedMessages, messageReceived);

        var partnerId = 77;
        await connectionManager.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = bootstrapServers
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderPickedUpConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderPickedUpConsumer>>().Object,
            config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = bootstrapServers });
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
        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));

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
        // Arrange - Only the partner who made the order should be notified
        await using var kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        await kafkaContainer.StartAsync();
        var bootstrapServers = kafkaContainer.GetBootstrapAddress();

        var connectionManager = new PartnerConnectionManager(
            new Mock<ILogger<PartnerConnectionManager>>().Object);

        // Two partners connected
        var partner1Messages = new List<string>();
        var partner2Messages = new List<string>();
        var signal = new TaskCompletionSource<bool>();

        var socket1 = CreateCapturingWebSocketMock(partner1Messages, signal);
        var socket2 = CreateCapturingWebSocketMock(partner2Messages, null);

        await connectionManager.RegisterConnectionAsync(1, socket1.Object);
        await connectionManager.RegisterConnectionAsync(2, socket2.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = bootstrapServers
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderPickedUpConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderPickedUpConsumer>>().Object,
            config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(6));

        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = bootstrapServers });
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
        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5)));

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
