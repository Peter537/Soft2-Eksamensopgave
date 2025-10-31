using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices
{
    public class OrderDeliveredConsumer : BackgroundService
    {
        private readonly ILogger<OrderDeliveredConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationRepository _repository;

        public OrderDeliveredConsumer(
            ILogger<OrderDeliveredConsumer> logger,
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
                KafkaTopics.OrderDelivered
            );

            await consumerService.ConsumeAsync<OrderDeliveredEvent>(async (orderEvent) =>
            {
                var notification = new Notification
                {
                    OrderId = orderEvent.OrderId,
                    Type = "OrderDelivered",
                    Message = "Your food has been delivered! Enjoy your meal! �️",
                    Emoji = "🎉",
                    Timestamp = orderEvent.DeliveredAt
                };

                _repository.AddNotification(notification);

                await Task.CompletedTask;
            });
        }
    }
}
