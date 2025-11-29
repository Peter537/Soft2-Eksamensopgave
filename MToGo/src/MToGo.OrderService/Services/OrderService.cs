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
        Task<bool> AcceptOrderAsync(int orderId);
        Task<bool> RejectOrderAsync(int orderId, string? reason);
        Task<bool> SetReadyAsync(int orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IPartnerServiceClient _partnerServiceClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IKafkaProducer kafkaProducer,
            IPartnerServiceClient partnerServiceClient,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
            _partnerServiceClient = partnerServiceClient;
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

        public async Task<bool> AcceptOrderAsync(int orderId)
        {
            _logger.AcceptingOrder(orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotAcceptOrder(orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Placed)
            {
                _logger.CannotAcceptOrder(orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Accepted;
            await _orderRepository.UpdateOrderAsync(order);

            // Audit log
            _logger.OrderAccepted(order.Id, order.CustomerId);

            // Fetch partner information
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty; // TODO: Null checks pga manglende Service
            var partnerAddress = partner?.Location ?? string.Empty; // TODO: Null checks pga manglende Service

            // Publish OrderAcceptedEvent
            var orderEvent = new OrderAcceptedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                PartnerName = partnerName,
                PartnerAddress = partnerAddress,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryFee = order.DeliveryFee,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Items = order.Items.Select(i => new OrderAcceptedEvent.OrderAcceptedItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderAccepted, order.Id.ToString(), orderEvent);
            _logger.PublishedOrderAcceptedEvent(order.Id);

            return true;
        }

        public async Task<bool> RejectOrderAsync(int orderId, string? reason)
        {
            _logger.RejectingOrder(orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotRejectOrder(orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Placed)
            {
                _logger.CannotRejectOrder(orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Rejected;
            await _orderRepository.UpdateOrderAsync(order);

            // Sanitize user input to prevent log injection attacks
            var sanitizedReason = (reason ?? "No reason provided")
                .Replace("\r", "")
                .Replace("\n", " ");

            // Audit log
            _logger.OrderRejected(order.Id, order.CustomerId, sanitizedReason);

            // Publish OrderRejectedEvent
            var orderEvent = new OrderRejectedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Reason = sanitizedReason,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderRejected, order.Id.ToString(), orderEvent);
            _logger.PublishedOrderRejectedEvent(order.Id);

            // TODO: Måske Refund Process logic her?

            return true;
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

        public async Task<bool> SetReadyAsync(int orderId)
        {
            _logger.SettingOrderReady(orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotSetOrderReady(orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Accepted)
            {
                _logger.CannotSetOrderReady(orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Ready;
            await _orderRepository.UpdateOrderAsync(order);

            // Audit log
            _logger.OrderSetReady(order.Id, order.CustomerId);

            // Fetch partner information
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty; // TODO: Null checks pga manglende Service
            var partnerAddress = partner?.Location ?? string.Empty; // TODO: Null checks pga manglende Service

            // Publish OrderReadyEvent
            var orderEvent = new OrderReadyEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                PartnerName = partnerName,
                PartnerAddress = partnerAddress,
                AgentId = order.AgentId,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderReady, order.Id.ToString(), orderEvent);
            _logger.PublishedOrderReadyEvent(order.Id);

            return true;
        }
    }
}