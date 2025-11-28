using MToGo.OrderService.Entities;
using MToGo.OrderService.Logging;
using MToGo.OrderService.Models;
using MToGo.OrderService.Repositories;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.OrderService.Services
{
    public interface IOrderService
    {
        Task<OrderCreateResponse> CreateOrderAsync(OrderCreateRequest request);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IKafkaProducer kafkaProducer, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        public async Task<OrderCreateResponse> CreateOrderAsync(OrderCreateRequest request)
        {
            _logger.CreatingOrder(request.CustomerId, request.PartnerId);

            // Calculate order total
            decimal orderTotal = request.Items.Sum(item => item.Quantity * item.UnitPrice);
            _logger.CalculatedOrderTotal(orderTotal, request.Items.Count);

            // Calculate service fee: 6% for <=100 DKK, 3% for >=1000 DKK, sliding in between
            decimal serviceFee = CalculateServiceFee(orderTotal);

            // Create order entity
            var order = new Order
            {
                CustomerId = request.CustomerId,
                PartnerId = request.PartnerId,
                DeliveryAddress = request.DeliveryAddress,
                DeliveryFee = request.DeliveryFee,
                ServiceFee = serviceFee,
                TotalAmount = orderTotal + serviceFee + request.DeliveryFee,
                Items = request.Items.Select(i => new OrderItem
                {
                    FoodItemId = i.FoodItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            var createdOrder = await _orderRepository.CreateOrderAsync(order);

            // Audit log
            _logger.OrderCreated(createdOrder.Id, createdOrder.CustomerId, createdOrder.PartnerId, createdOrder.TotalAmount);

            // Publish OrderCreatedEvent
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = createdOrder.Id,
                PartnerId = request.PartnerId,
                OrderCreatedTime = createdOrder.CreatedAt.ToString("O"),
                Items = request.Items.Select(i => new OrderCreatedEvent.OrderCreatedItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderCreated, createdOrder.Id.ToString(), orderEvent);
            _logger.PublishedOrderCreatedEvent(createdOrder.Id);

            return new OrderCreateResponse { Id = createdOrder.Id };
        }

        private decimal CalculateServiceFee(decimal orderTotal)
        {
            if (orderTotal <= 100)
                return orderTotal * 0.06m;
            if (orderTotal >= 1000)
                return orderTotal * 0.03m;

            decimal rate = 0.06m - (orderTotal - 100) / 900m * 0.03m;
            return orderTotal * rate;
        }
    }
}