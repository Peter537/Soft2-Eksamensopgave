using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices
{
    public class OrderPreparingConsumer : BackgroundService
    {
        private readonly ILogger<OrderPreparingConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationRepository _repository;

        public OrderPreparingConsumer(
            ILogger<OrderPreparingConsumer> logger,
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
                KafkaTopics.OrderPreparing
            );

            await consumerService.ConsumeAsync<OrderPreparingEvent>(async (orderEvent) =>
            {
                var notification = new Notification
                {
                    OrderId = orderEvent.OrderId,
                    Type = "OrderPreparing",
                    Message = "Your food is being prepared! 🍳",
                    Emoji = "👨‍🍳",
                    Timestamp = orderEvent.StartedAt
                };

                _repository.AddNotification(notification);

                await Task.CompletedTask;
            });
        }
    }
}
