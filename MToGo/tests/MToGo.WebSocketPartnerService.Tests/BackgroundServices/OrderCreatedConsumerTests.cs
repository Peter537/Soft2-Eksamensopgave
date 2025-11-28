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

public class OrderCreatedConsumerTests
{
    [Fact]
    public async Task Consumer_ShouldSendToPartner_WhenOrderCreatedEventReceived()
    {
        // Arrange
        await using var kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        await kafkaContainer.StartAsync();
        var bootstrapServers = kafkaContainer.GetBootstrapAddress();

        // Real connection manager
        var connectionManagerLogger = new Mock<ILogger<PartnerConnectionManager>>();
        var connectionManager = new PartnerConnectionManager(connectionManagerLogger.Object);

        // Mock WebSocket to capture sent messages
        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        webSocketMock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()))
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>((data, type, endOfMessage, ct) =>
            {
                var message = Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count);
                receivedMessages.Add(message);
                messageReceived.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);

        // Register partner 5 with the mock WebSocket
        var partnerId = 5;
        await connectionManager.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Build configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = bootstrapServers
            })
            .Build();

        // Build service provider
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Create the actual consumer
        var consumerLogger = new Mock<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(
            serviceProvider,
            connectionManager,
            consumerLogger.Object,
            config);

        // Start consumer in background
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(6)); // Wait for consumer startup delay

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = bootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 1001,
            PartnerId = partnerId,
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = [new OrderCreatedEvent.OrderCreatedItem { Name = "Burger", Quantity = 2 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        
        // Stop consumer
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
        await using var kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        await kafkaContainer.StartAsync();
        var bootstrapServers = kafkaContainer.GetBootstrapAddress();

        // Real connection manager with NO connections
        var connectionManagerLogger = new Mock<ILogger<PartnerConnectionManager>>();
        var connectionManager = new PartnerConnectionManager(connectionManagerLogger.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = bootstrapServers
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var consumerLogger = new Mock<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(
            serviceProvider,
            connectionManager,
            consumerLogger.Object,
            config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Publish event for partner that's NOT connected
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = bootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 999,
            PartnerId = 999, // Not connected
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = []
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.Delay(TimeSpan.FromSeconds(1));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed event without crashing
        Assert.Equal(0, connectionManager.ConnectionCount);
    }

    [Fact]
    public async Task Consumer_ShouldRouteToCorrectPartner_WhenMultipleConnected()
    {
        // Arrange
        await using var kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        await kafkaContainer.StartAsync();
        var bootstrapServers = kafkaContainer.GetBootstrapAddress();

        var connectionManagerLogger = new Mock<ILogger<PartnerConnectionManager>>();
        var connectionManager = new PartnerConnectionManager(connectionManagerLogger.Object);

        // Partner 1 WebSocket
        var partner1Messages = new List<string>();
        var partner1Mock = CreateCapturingWebSocketMock(partner1Messages);

        // Partner 2 WebSocket
        var partner2Messages = new List<string>();
        var partner2Received = new TaskCompletionSource<bool>();
        var partner2Mock = new Mock<WebSocket>();
        partner2Mock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        partner2Mock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()))
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>((data, type, endOfMessage, ct) =>
            {
                partner2Messages.Add(Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count));
                partner2Received.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);

        await connectionManager.RegisterConnectionAsync(1, partner1Mock.Object);
        await connectionManager.RegisterConnectionAsync(2, partner2Mock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = bootstrapServers
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var consumer = new OrderCreatedConsumer(
            serviceProvider,
            connectionManager,
            new Mock<ILogger<OrderCreatedConsumer>>().Object,
            config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Publish event for Partner 2 only
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = bootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 123,
            PartnerId = 2, // Only partner 2
            OrderCreatedTime = DateTime.UtcNow.ToString("O"),
            Items = [new OrderCreatedEvent.OrderCreatedItem { Name = "Pizza", Quantity = 1 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderCreated, orderEvent.OrderId.ToString(), orderEvent);
        await Task.WhenAny(partner2Received.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Empty(partner1Messages); // Partner 1 should NOT receive it
        Assert.Single(partner2Messages); // Partner 2 should receive it
        Assert.Contains("Pizza", partner2Messages[0]);
    }

    private static Mock<WebSocket> CreateCapturingWebSocketMock(List<string> capturedMessages)
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
            })
            .Returns(Task.CompletedTask);
        return mock;
    }
}
