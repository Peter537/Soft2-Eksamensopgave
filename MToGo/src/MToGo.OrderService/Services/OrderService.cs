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
        Task<bool> AcceptOrderAsync(int orderId, int estimatedMinutes);
        Task<bool> RejectOrderAsync(int orderId, string? reason);
        Task<bool> SetReadyAsync(int orderId);
        Task<AssignAgentResult> AssignAgentAsync(int orderId, int agentId);
        Task<PickupResult> PickupOrderAsync(int orderId);
        Task<DeliveryResult> CompleteDeliveryAsync(int orderId);
        Task<List<CustomerOrderResponse>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AgentDeliveryResponse>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<PartnerOrderResponse>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public enum AssignAgentResult
    {
        Success,
        OrderNotFound,
        InvalidStatus,
        AgentAlreadyAssigned
    }

    public enum PickupResult
    {
        Success,
        OrderNotFound,
        InvalidStatus,
        NoAgentAssigned
    }

    public enum DeliveryResult
    {
        Success,
        OrderNotFound,
        InvalidStatus,
        NoAgentAssigned
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IPartnerServiceClient _partnerServiceClient;
        private readonly IAgentServiceClient _agentServiceClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IKafkaProducer kafkaProducer,
            IPartnerServiceClient partnerServiceClient,
            IAgentServiceClient agentServiceClient,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
            _partnerServiceClient = partnerServiceClient;
            _agentServiceClient = agentServiceClient;
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
                Distance = request.Distance,
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
                Distance = createdOrder.Distance,
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

        public async Task<bool> AcceptOrderAsync(int orderId, int estimatedMinutes)
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
            order.EstimatedMinutes = estimatedMinutes;
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
                Distance = order.Distance,
                EstimatedMinutes = order.EstimatedMinutes,
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

        public async Task<AssignAgentResult> AssignAgentAsync(int orderId, int agentId)
        {
            _logger.AssigningAgent(orderId, agentId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotAssignAgent(orderId, "Order not found");
                return AssignAgentResult.OrderNotFound;
            }

            // Allow agent assignment when order is Accepted OR Ready
            if (order.Status != OrderStatus.Accepted && order.Status != OrderStatus.Ready)
            {
                _logger.CannotAssignAgent(orderId, $"Invalid status: {order.Status}");
                return AssignAgentResult.InvalidStatus;
            }

            if (order.AgentId != null)
            {
                _logger.CannotAssignAgent(orderId, $"Agent already assigned: {order.AgentId}");
                return AssignAgentResult.AgentAlreadyAssigned;
            }

            order.AgentId = agentId;
            await _orderRepository.UpdateOrderAsync(order);

            // Audit log
            _logger.AgentAssigned(order.Id, order.PartnerId, agentId);

            // Fetch partner information for the agent's view
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty;
            var partnerAddress = partner?.Location ?? string.Empty;

            // Publish AgentAssignedEvent with full order details
            var orderEvent = new AgentAssignedEvent
            {
                OrderId = order.Id,
                PartnerId = order.PartnerId,
                AgentId = agentId,
                Timestamp = DateTime.UtcNow.ToString("O"),
                PartnerName = partnerName,
                PartnerAddress = partnerAddress,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryFee = order.DeliveryFee,
                Items = order.Items.Select(i => new AgentAssignedEvent.AgentAssignedItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.AgentAssigned, order.Id.ToString(), orderEvent);
            _logger.PublishedAgentAssignedEvent(order.Id);

            return AssignAgentResult.Success;
        }

        public async Task<PickupResult> PickupOrderAsync(int orderId)
        {
            _logger.PickingUpOrder(orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotPickupOrder(orderId, "Order not found");
                return PickupResult.OrderNotFound;
            }

            if (order.Status != OrderStatus.Ready)
            {
                _logger.CannotPickupOrder(orderId, $"Invalid status: {order.Status}");
                return PickupResult.InvalidStatus;
            }

            if (order.AgentId == null)
            {
                _logger.CannotPickupOrder(orderId, "No agent assigned");
                return PickupResult.NoAgentAssigned;
            }

            order.Status = OrderStatus.PickedUp;
            await _orderRepository.UpdateOrderAsync(order);

            // Fetch agent information
            var agent = await _agentServiceClient.GetAgentByIdAsync(order.AgentId.Value);
            var agentName = agent?.Name ?? string.Empty; // TODO: Null checks pga manglende Service

            // Audit log
            _logger.OrderPickedUp(order.Id, order.CustomerId, agentName);

            // Publish OrderPickedUpEvent
            var orderEvent = new OrderPickedUpEvent
            {
                OrderId = order.Id,
                PartnerId = order.PartnerId,
                CustomerId = order.CustomerId,
                AgentName = agentName,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderPickedUp, order.Id.ToString(), orderEvent);
            _logger.PublishedOrderPickedUpEvent(order.Id);

            return PickupResult.Success;
        }

        public async Task<DeliveryResult> CompleteDeliveryAsync(int orderId)
        {
            _logger.CompletingDelivery(orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.CannotCompleteDelivery(orderId, "Order not found");
                return DeliveryResult.OrderNotFound;
            }

            if (order.Status != OrderStatus.PickedUp)
            {
                _logger.CannotCompleteDelivery(orderId, $"Invalid status: {order.Status}");
                return DeliveryResult.InvalidStatus;
            }

            if (order.AgentId == null)
            {
                _logger.CannotCompleteDelivery(orderId, "No agent assigned");
                return DeliveryResult.NoAgentAssigned;
            }

            order.Status = OrderStatus.Delivered;
            await _orderRepository.UpdateOrderAsync(order);

            // Audit log
            _logger.OrderDelivered(order.Id, order.CustomerId);

            // Publish OrderDeliveredEvent
            var orderEvent = new OrderDeliveredEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderDelivered, order.Id.ToString(), orderEvent);
            _logger.PublishedOrderDeliveredEvent(order.Id);

            return DeliveryResult.Success;
        }

        public async Task<List<CustomerOrderResponse>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.GettingOrderHistory(customerId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, startDate, endDate);

            _logger.OrderHistoryRetrieved(customerId, orders.Count);

            return orders.Select(o => new CustomerOrderResponse
            {
                Id = o.Id,
                PartnerId = o.PartnerId,
                AgentId = o.AgentId,
                DeliveryAddress = o.DeliveryAddress,
                ServiceFee = o.ServiceFee,
                DeliveryFee = o.DeliveryFee,
                Status = o.Status.ToString(),
                OrderCreatedTime = o.CreatedAt.ToString("O"),
                Items = o.Items.Select(i => new CustomerOrderItemResponse
                {
                    FoodItemId = i.FoodItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToList();
        }

        public async Task<List<AgentDeliveryResponse>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.GettingAgentDeliveryHistory(agentId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByAgentIdAsync(agentId, startDate, endDate);

            _logger.AgentDeliveryHistoryRetrieved(agentId, orders.Count);

            return orders.Select(o => new AgentDeliveryResponse
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                PartnerId = o.PartnerId,
                DeliveryAddress = o.DeliveryAddress,
                ServiceFee = o.ServiceFee,
                DeliveryFee = o.DeliveryFee,
                Status = o.Status.ToString(),
                OrderCreatedTime = o.CreatedAt.ToString("O"),
                Items = o.Items.Select(i => new AgentDeliveryItemResponse
                {
                    FoodItemId = i.FoodItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToList();
        }

        public async Task<List<PartnerOrderResponse>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.GettingPartnerOrderHistory(partnerId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByPartnerIdAsync(partnerId, startDate, endDate);

            _logger.PartnerOrderHistoryRetrieved(partnerId, orders.Count);

            return orders.Select(o => new PartnerOrderResponse
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                AgentId = o.AgentId,
                DeliveryAddress = o.DeliveryAddress,
                ServiceFee = o.ServiceFee,
                DeliveryFee = o.DeliveryFee,
                Status = o.Status.ToString(),
                OrderCreatedTime = o.CreatedAt.ToString("O"),
                Items = o.Items.Select(i => new PartnerOrderItemResponse
                {
                    FoodItemId = i.FoodItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToList();
        }
    }
}