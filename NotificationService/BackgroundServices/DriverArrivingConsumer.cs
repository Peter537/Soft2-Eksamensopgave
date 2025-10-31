using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices;

public class DriverArrivingConsumer : BackgroundService
{
    private readonly ILogger<DriverArrivingConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly NotificationRepository _repository;

    public DriverArrivingConsumer(
        ILogger<DriverArrivingConsumer> logger,
        IConfiguration configuration,
        NotificationRepository repository)
    {
        _logger = logger;
        _configuration = configuration;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var consumerService = new KafkaConsumerService(
            bootstrapServers,
            "notification-service-group",
            KafkaTopics.DriverArriving
        );

        await consumerService.ConsumeAsync<DriverArrivingEvent>(async (driverEvent) =>
        {
            var notification = new Notification
            {
                OrderId = driverEvent.OrderId,
                Type = "DriverArriving",
                Message = $"Driver is arriving soon! Estimated {driverEvent.EstimatedMinutes} minutes away 🚗",
                Emoji = "⏰",
                Timestamp = driverEvent.Timestamp
            };

            _repository.AddNotification(notification);

            await Task.CompletedTask;
        });
    }
}
