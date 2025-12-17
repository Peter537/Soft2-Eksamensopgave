using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MToGo.NotificationService.BackgroundServices;
using MToGo.NotificationService.Models;
using MToGo.NotificationService.Services;
using MToGo.NotificationService.Tests.Fixtures;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.NotificationService.Tests.BackgroundServices;

[Collection("Kafka")]
public class OrderAcceptedNotificationConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderAcceptedNotificationConsumerTests(SharedKafkaFixture kafkaFixture)
    {
        _kafkaFixture = kafkaFixture;
    }

    private IConfiguration CreateConfig(string? groupIdSuffix = null)
    {
        var uniqueGroupId = $"notification-service-order-accepted-test-{groupIdSuffix ?? Guid.NewGuid().ToString()}";
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = _kafkaFixture.BootstrapServers,
                ["Kafka:GroupId:OrderAccepted"] = uniqueGroupId
            })
            .Build();
    }

    [Fact]
    public async Task Consumer_ShouldSendNotification_WhenOrderAcceptedEventReceived()
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N")[..8];
        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        var expectedCustomerId = 42;
        
        var receivedRequests = new List<NotificationRequest>();
        var notificationSent = new TaskCompletionSource<NotificationRequest>();
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(req => 
            {
                receivedRequests.Add(req);
                // Only signal completion for our specific message
                if (req.Message.Contains($"#{uniqueOrderId}"))
                    notificationSent.TrySetResult(req);
            })
            .ReturnsAsync(new NotificationResponse { Success = true, Message = "Sent" });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => mockNotificationService.Object);

        var consumer = new OrderAcceptedNotificationConsumer(
            services.BuildServiceProvider(),
            new Mock<ILogger<OrderAcceptedNotificationConsumer>>().Object,
            CreateConfig(testId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for consumer to be ready

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderAcceptedEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = expectedCustomerId,
            PartnerName = "Pizza Palace",
            PartnerAddress = "123 Main St",
            DeliveryAddress = "456 Oak Ave",
            DeliveryFee = 5.00m,
            Distance = "2.5 km",
            EstimatedMinutes = 20,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = [new OrderAcceptedEvent.OrderAcceptedItem { Name = "Margherita", Quantity = 1 }]
        };

        await producer.PublishAsync(KafkaTopics.OrderAccepted, orderEvent.OrderId.ToString(), orderEvent);

        // Wait for notification
        var completedTask = await Task.WhenAny(notificationSent.Task, Task.Delay(8000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Equal(notificationSent.Task, completedTask);
        var request = await notificationSent.Task;
        Assert.Equal(expectedCustomerId, request.CustomerId);
        Assert.Contains($"#{uniqueOrderId}", request.Message);
        Assert.Contains("Pizza Palace", request.Message);
        Assert.Contains("20 minutes", request.Message);
    }

    [Fact]
    public async Task Consumer_ShouldNotFail_WhenNotificationServiceThrows()
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N")[..8];
        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        var invocationReceived = new TaskCompletionSource<bool>();
        
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(req =>
            {
                if (req.Message.Contains($"#{uniqueOrderId}"))
                    invocationReceived.TrySetResult(true);
            })
            .ThrowsAsync(new Exception("Notification failed"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => mockNotificationService.Object);

        var mockLogger = new Mock<ILogger<OrderAcceptedNotificationConsumer>>();

        var consumer = new OrderAcceptedNotificationConsumer(
            services.BuildServiceProvider(),
            mockLogger.Object,
            CreateConfig(testId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(2000);

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderAcceptedEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = 1,
            PartnerName = "Test Partner",
            EstimatedMinutes = 15,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Items = []
        };

        // Act - should not throw even when notification fails
        await producer.PublishAsync(KafkaTopics.OrderAccepted, orderEvent.OrderId.ToString(), orderEvent);
        
        // Wait for our specific message to be processed
        var completed = await Task.WhenAny(invocationReceived.Task, Task.Delay(8000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed our specific event without crashing
        Assert.Equal(invocationReceived.Task, completed);
    }
}

