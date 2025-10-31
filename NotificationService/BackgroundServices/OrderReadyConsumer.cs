using Shared.Events;
using Shared.Kafka;
using NotificationService.Models;

namespace NotificationService.BackgroundServices
{
    public class OrderReadyConsumer : BackgroundService
    {
        private readonly ILogger<OrderReadyConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationRepository _repository;

        public OrderReadyConsumer(
            ILogger<OrderReadyConsumer> logger,
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
                KafkaTopics.OrderReady
            );

            await consumerService.ConsumeAsync<OrderReadyEvent>(async (orderEvent) =>
            {
                var notification = new Notification
                {
                    OrderId = orderEvent.OrderId,
                    Type = "OrderReady",
                    Message = "Your order is ready for pickup! 🎉",
                    Emoji = "🍕",
                    Timestamp = orderEvent.ReadyAt
                };

                _repository.AddNotification(notification);

                await Task.CompletedTask;
            });
        }
    }
}
