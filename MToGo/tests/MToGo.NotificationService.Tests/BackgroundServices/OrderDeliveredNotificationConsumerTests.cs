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
public class OrderDeliveredNotificationConsumerTests
{
    private readonly SharedKafkaFixture _kafkaFixture;

    public OrderDeliveredNotificationConsumerTests(SharedKafkaFixture kafkaFixture)
    {
        _kafkaFixture = kafkaFixture;
    }

    private IConfiguration CreateConfig(string? groupIdSuffix = null)
    {
        var uniqueGroupId = $"notification-service-order-delivered-test-{groupIdSuffix ?? Guid.NewGuid().ToString()}";
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = _kafkaFixture.BootstrapServers,
                ["Kafka:GroupId:OrderDelivered"] = uniqueGroupId
            })
            .Build();
    }

    [Fact]
    public async Task Consumer_ShouldSendNotification_WhenOrderDeliveredEventReceived()
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N")[..8];
        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        var expectedCustomerId = 88;
        
        var notificationSent = new TaskCompletionSource<NotificationRequest>();
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(req =>
            {
                if (req.Message.Contains($"#{uniqueOrderId}"))
                    notificationSent.TrySetResult(req);
            })
            .ReturnsAsync(new NotificationResponse { Success = true, Message = "Sent" });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => mockNotificationService.Object);

        var consumer = new OrderDeliveredNotificationConsumer(
            services.BuildServiceProvider(),
            new Mock<ILogger<OrderDeliveredNotificationConsumer>>().Object,
            CreateConfig(testId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for consumer to be ready

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderDeliveredEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = expectedCustomerId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderDelivered, orderEvent.OrderId.ToString(), orderEvent);

        // Wait for notification
        var completedTask = await Task.WhenAny(notificationSent.Task, Task.Delay(8000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Equal(notificationSent.Task, completedTask);
        var request = await notificationSent.Task;
        Assert.Equal(expectedCustomerId, request.CustomerId);
        Assert.Contains($"#{uniqueOrderId}", request.Message);
        Assert.Contains("delivered", request.Message);
        Assert.Contains("Enjoy your meal", request.Message);
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

        var consumer = new OrderDeliveredNotificationConsumer(
            services.BuildServiceProvider(),
            new Mock<ILogger<OrderDeliveredNotificationConsumer>>().Object,
            CreateConfig(testId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(2000);

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderDeliveredEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = 1,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Act - should not throw
        await producer.PublishAsync(KafkaTopics.OrderDelivered, orderEvent.OrderId.ToString(), orderEvent);
        
        var completed = await Task.WhenAny(invocationReceived.Task, Task.Delay(8000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert - consumer processed our specific event without crashing
        Assert.Equal(invocationReceived.Task, completed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(999)]
    public async Task Consumer_ShouldSendNotificationToCorrectCustomer(int customerId)
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N")[..8];
        var uniqueOrderId = Random.Shared.Next(100000, 999999);
        
        var notificationSent = new TaskCompletionSource<NotificationRequest>();
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(req =>
            {
                if (req.Message.Contains($"#{uniqueOrderId}"))
                    notificationSent.TrySetResult(req);
            })
            .ReturnsAsync(new NotificationResponse { Success = true, Message = "Sent" });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => mockNotificationService.Object);

        var consumer = new OrderDeliveredNotificationConsumer(
            services.BuildServiceProvider(),
            new Mock<ILogger<OrderDeliveredNotificationConsumer>>().Object,
            CreateConfig(testId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await consumer.StartAsync(cts.Token);
        await Task.Delay(2000);

        // Publish event
        var producerConfig = Options.Create(new KafkaProducerConfig { BootstrapServers = _kafkaFixture.BootstrapServers });
        await using var producer = new KafkaProducer(producerConfig, new LoggerFactory().CreateLogger<KafkaProducer>());

        var orderEvent = new OrderDeliveredEvent
        {
            OrderId = uniqueOrderId,
            CustomerId = customerId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        await producer.PublishAsync(KafkaTopics.OrderDelivered, orderEvent.OrderId.ToString(), orderEvent);

        // Wait for notification
        var completedTask = await Task.WhenAny(notificationSent.Task, Task.Delay(8000));

        cts.Cancel();
        try { await consumer.StopAsync(CancellationToken.None); } catch { }

        // Assert
        Assert.Equal(notificationSent.Task, completedTask);
        var request = await notificationSent.Task;
        Assert.Equal(customerId, request.CustomerId);
    }
}
