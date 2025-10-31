using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices
{
    public class OrderAcceptedConsumer : BackgroundService
    {
        private readonly ILogger<OrderAcceptedConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationRepository _repository;

        public OrderAcceptedConsumer(
            ILogger<OrderAcceptedConsumer> logger,
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
                KafkaTopics.OrderAccepted
            );

            await consumerService.ConsumeAsync<OrderAcceptedEvent>(async (orderEvent) =>
            {
                // Create notification
                var notification = new Notification
                {
                    OrderId = orderEvent.OrderId,
                    Type = "OrderAccepted",
                    Message = $"Your order has been accepted by the restaurant!",
                    Emoji = "✅",
                    Timestamp = orderEvent.AcceptedAt
                };

                _repository.AddNotification(notification);

                await Task.CompletedTask;
            });
        }
    }
}
