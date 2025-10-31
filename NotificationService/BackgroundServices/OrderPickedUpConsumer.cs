using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices
{
    public class OrderPickedUpConsumer : BackgroundService
    {
        private readonly ILogger<OrderPickedUpConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationRepository _repository;

        public OrderPickedUpConsumer(
            ILogger<OrderPickedUpConsumer> logger,
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
                KafkaTopics.OrderPickedUp
            );

            await consumerService.ConsumeAsync<OrderPickedUpEvent>(async (orderEvent) =>
            {
                var notification = new Notification
                {
                    OrderId = orderEvent.OrderId,
                    Type = "OrderPickedUp",
                    Message = "Driver is on the way with your order! 🚗",
                    Emoji = "🚚",
                    Timestamp = orderEvent.PickedUpAt
                };

                _repository.AddNotification(notification);

                await Task.CompletedTask;
            });
        }
    }
}
