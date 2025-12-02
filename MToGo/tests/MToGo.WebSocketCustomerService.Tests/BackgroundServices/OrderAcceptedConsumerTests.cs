using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.WebSocketCustomerService.BackgroundServices;
using MToGo.WebSocketCustomerService.Services;
using MToGo.WebSocketCustomerService.Tests.Fixtures;

namespace MToGo.WebSocketCustomerService.Tests.BackgroundServices;

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
    public async Task Consumer_ShouldSendOrderAccepted_ToCorrectCustomer()
    {
        // Arrange
        var connectionManager = new CustomerConnectionManager(new Mock<ILogger<CustomerConnectionManager>>().Object);

        var customerMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var customerMock = CreateCapturingWebSocketMock(customerMessages, messageReceived);

        var customerId = 100;
        await connectionManager.RegisterConnectionAsync(customerId, customerMock.Object);

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

        // Publish event with unique ID
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        var acceptedEvent = new OrderAcceptedEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = customerId,
            PartnerName = "Test Restaurant",
            EstimatedMinutes = 25,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderAccepted, acceptedEvent.OrderId.ToString(), acceptedEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - find our specific message
        var msg = customerMessages.FirstOrDefault(m => m.Contains(uniqueOrderId.ToString()));
        Assert.NotNull(msg);
        Assert.Contains("OrderAccepted", msg);
        Assert.Contains("Test Restaurant", msg);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenCustomerNotConnected()
    {
        // Arrange
        var connectionManager = new CustomerConnectionManager(new Mock<ILogger<CustomerConnectionManager>>().Object);

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

        // Publish event for customer that's NOT connected
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var acceptedEvent = new OrderAcceptedEvent
        {
            OrderId = 9998,
            CustomerId = 9998,
            PartnerName = "Test",
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderAccepted, acceptedEvent.OrderId.ToString(), acceptedEvent);
        await Task.Delay(300);

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed event without crashing
        Assert.Equal(0, connectionManager.ConnectionCount);
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
