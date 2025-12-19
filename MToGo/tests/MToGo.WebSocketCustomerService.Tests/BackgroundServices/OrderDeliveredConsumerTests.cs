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
public class OrderDeliveredConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderDeliveredConsumerTests(SharedKafkaFixture kafkaFixture)
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
    public async Task Consumer_ShouldSendOrderDelivered_ToCorrectCustomer()
    {
        // Arrange
        var connectionManager = new CustomerConnectionManager(new Mock<ILogger<CustomerConnectionManager>>().Object);

        var customerMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();
        var customerMock = CreateCapturingWebSocketMock(customerMessages, messageReceived);

        var customerId = 200;
        await connectionManager.RegisterConnectionAsync(customerId, customerMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();

        var consumer = new OrderDeliveredConsumer(
            services.BuildServiceProvider(),
            connectionManager,
            new Mock<ILogger<OrderDeliveredConsumer>>().Object,
            CreateConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(1500);

        // Publish event with unique ID
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var uniqueOrderId = Random.Shared.Next(200000, 299999);
        var deliveredEvent = new OrderDeliveredEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = customerId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderDelivered, deliveredEvent.OrderId.ToString(), deliveredEvent);
        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        var msg = customerMessages.FirstOrDefault(m => m.Contains(uniqueOrderId.ToString()));
        Assert.NotNull(msg);
        Assert.Contains("OrderDelivered", msg);
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

