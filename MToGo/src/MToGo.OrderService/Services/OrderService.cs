using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Repositories;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.Shared.Logging;

namespace MToGo.OrderService.Services
{
    public interface IOrderService
    {
        /// <summary>
        /// Creates an order, calculates fees, persists it, and publishes an OrderCreated event.
        /// </summary>
        Task<OrderCreateResponse> CreateOrderAsync(OrderCreateRequest request);
        /// <summary>
        /// Accepts an order for preparation with an estimated ready time.
        /// </summary>
        Task<bool> AcceptOrderAsync(int orderId, int estimatedMinutes);
        /// <summary>
        /// Rejects an order with an optional reason and notifies downstream consumers.
        /// </summary>
        Task<bool> RejectOrderAsync(int orderId, string? reason);
        /// <summary>
        /// Marks an order as ready for pickup.
        /// </summary>
        Task<bool> SetReadyAsync(int orderId);
        /// <summary>
        /// Assigns a delivery agent and emits assignment events.
        /// </summary>
        Task<AssignAgentResult> AssignAgentAsync(int orderId, int agentId);
        /// <summary>
        /// Marks pickup by the assigned agent and publishes pickup events.
        /// </summary>
        Task<PickupResult> PickupOrderAsync(int orderId);
        /// <summary>
        /// Completes delivery and triggers completion events.
        /// </summary>
        Task<DeliveryResult> CompleteDeliveryAsync(int orderId);
        /// <summary>
        /// Lists orders for a customer within an optional date range.
        /// </summary>
        Task<List<CustomerOrderResponse>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// Lists deliveries handled by an agent within an optional date range.
        /// </summary>
        Task<List<AgentDeliveryResponse>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// Lists orders for a partner within an optional date range.
        /// </summary>
        Task<List<PartnerOrderResponse>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// Returns detailed order information with access control on user and role.
        /// </summary>
        Task<GetOrderDetailResult> GetOrderDetailAsync(int orderId, int userId, string userRole);
        /// <summary>
        /// Lists active (in-progress) orders for a customer.
        /// </summary>
        Task<List<CustomerOrderResponse>> GetActiveOrdersByCustomerIdAsync(int customerId);
        /// <summary>
        /// Lists active (in-progress) orders for a partner.
        /// </summary>
        Task<List<PartnerOrderResponse>> GetActiveOrdersByPartnerIdAsync(int partnerId);
        /// <summary>
        /// Lists active (in-progress) deliveries for an agent.
        /// </summary>
        Task<List<AgentDeliveryResponse>> GetActiveOrdersByAgentIdAsync(int agentId);
        /// <summary>
        /// Returns unassigned orders available for agents.
        /// </summary>
        Task<List<AvailableJobResponse>> GetAvailableOrdersAsync();
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

        /// <summary>
        /// Creates and persists an order, calculates fees, audits, and publishes the OrderCreated event.
        /// </summary>
        public async Task<OrderCreateResponse> CreateOrderAsync(OrderCreateRequest request)
        {
            _logger.LogInformation("Creating order for CustomerId: {CustomerId}, PartnerId: {PartnerId}", request.CustomerId, request.PartnerId);

            // Calculate order total
            decimal orderTotal = request.Items.Sum(item => item.Quantity * item.UnitPrice);
            _logger.LogDebug("Calculated order total: {OrderTotal} DKK for {ItemCount} items", orderTotal, request.Items.Count);

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

            _logger.LogAuditInformation(
                action: "OrderCreated",
                resource: "Order",
                resourceId: createdOrder.Id.ToString(),
                userId: createdOrder.CustomerId,
                userRole: "Customer",
                message: "Order created: OrderId={OrderId}, CustomerId={CustomerId}, PartnerId={PartnerId}, TotalAmount={TotalAmount} DKK",
                args: new object[] { createdOrder.Id, createdOrder.CustomerId, createdOrder.PartnerId, createdOrder.TotalAmount });

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
            _logger.LogDebug("Published OrderCreatedEvent to Kafka for OrderId: {OrderId}", createdOrder.Id);

            return new OrderCreateResponse { Id = createdOrder.Id };
        }

        /// <summary>
        /// Accepts an order for preparation and records the estimated ready time.
        /// </summary>
        public async Task<bool> AcceptOrderAsync(int orderId, int estimatedMinutes)
        {
            _logger.LogInformation("Accepting order: OrderId={OrderId}", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot accept order: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Placed)
            {
                _logger.LogWarning("Cannot accept order: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Accepted;
            order.EstimatedMinutes = estimatedMinutes;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogAuditInformation(
                action: "OrderAccepted",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: order.PartnerId,
                userRole: "Partner",
                message: "Order accepted: OrderId={OrderId}, CustomerId={CustomerId}, PartnerId={PartnerId}",
                args: new object[] { order.Id, order.CustomerId, order.PartnerId });

            // Fetch partner information
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty;
            var partnerAddress = partner?.Address ?? string.Empty;

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
            _logger.LogDebug("Published OrderAcceptedEvent to Kafka for OrderId: {OrderId}", order.Id);

            return true;
        }

        /// <summary>
        /// Rejects an order, records the reason, and emits rejection events.
        /// </summary>
        public async Task<bool> RejectOrderAsync(int orderId, string? reason)
        {
            _logger.LogInformation("Rejecting order: OrderId={OrderId}", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot reject order: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Placed)
            {
                _logger.LogWarning("Cannot reject order: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Rejected;
            await _orderRepository.UpdateOrderAsync(order);

            // Sanitize user input to prevent log injection attacks
            var sanitizedReason = (reason ?? "No reason provided")
                .Replace("\r", "")
                .Replace("\n", " ");

            _logger.LogAuditInformation(
                action: "OrderRejected",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: order.PartnerId,
                userRole: "Partner",
                message: "Order rejected: OrderId={OrderId}, CustomerId={CustomerId}, Reason={Reason}",
                args: new object[] { order.Id, order.CustomerId, sanitizedReason });

            // Publish OrderRejectedEvent
            var orderEvent = new OrderRejectedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Reason = sanitizedReason,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderRejected, order.Id.ToString(), orderEvent);
            _logger.LogDebug("Published OrderRejectedEvent to Kafka for OrderId: {OrderId}", order.Id);

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

        /// <summary>
        /// Marks an order as ready for pickup and publishes readiness events.
        /// </summary>
        public async Task<bool> SetReadyAsync(int orderId)
        {
            _logger.LogInformation("Setting order ready: OrderId={OrderId}", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot set order ready: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return false;
            }

            if (order.Status != OrderStatus.Accepted)
            {
                _logger.LogWarning("Cannot set order ready: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Ready;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogAuditInformation(
                action: "OrderReady",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: order.PartnerId,
                userRole: "Partner",
                message: "Order set ready: OrderId={OrderId}, CustomerId={CustomerId}",
                args: new object[] { order.Id, order.CustomerId });

            // Fetch partner information
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty;
            var partnerAddress = partner?.Address ?? string.Empty;

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
            _logger.LogDebug("Published OrderReadyEvent to Kafka for OrderId: {OrderId}", order.Id);

            return true;
        }

        /// <summary>
        /// Assigns an agent to an order, updates status, and publishes assignment events.
        /// </summary>
        public async Task<AssignAgentResult> AssignAgentAsync(int orderId, int agentId)
        {
            _logger.LogInformation("Assigning agent to order: OrderId={OrderId}, AgentId={AgentId}", orderId, agentId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot assign agent to order: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return AssignAgentResult.OrderNotFound;
            }

            // Allow agent assignment when order is Accepted OR Ready
            if (order.Status != OrderStatus.Accepted && order.Status != OrderStatus.Ready)
            {
                _logger.LogWarning("Cannot assign agent to order: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return AssignAgentResult.InvalidStatus;
            }

            if (order.AgentId != null)
            {
                _logger.LogWarning("Cannot assign agent to order: OrderId={OrderId}, Reason={Reason}", orderId, $"Agent already assigned: {order.AgentId}");
                return AssignAgentResult.AgentAlreadyAssigned;
            }

            order.AgentId = agentId;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogAuditInformation(
                action: "AgentAssigned",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: agentId,
                userRole: "Agent",
                message: "Agent assigned to order: OrderId={OrderId}, PartnerId={PartnerId}, AgentId={AgentId}",
                args: new object[] { order.Id, order.PartnerId, agentId });

            // Fetch partner information for the agent's view
            var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);
            var partnerName = partner?.Name ?? string.Empty;
            var partnerAddress = partner?.Address ?? string.Empty;

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
            _logger.LogDebug("Published AgentAssignedEvent to Kafka for OrderId: {OrderId}", order.Id);

            return AssignAgentResult.Success;
        }

        /// <summary>
        /// Confirms agent pickup, timestamps the pickup, and publishes pickup events.
        /// </summary>
        public async Task<PickupResult> PickupOrderAsync(int orderId)
        {
            _logger.LogInformation("Picking up order: OrderId={OrderId}", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot pickup order: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return PickupResult.OrderNotFound;
            }

            if (order.Status != OrderStatus.Ready)
            {
                _logger.LogWarning("Cannot pickup order: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return PickupResult.InvalidStatus;
            }

            if (order.AgentId == null)
            {
                _logger.LogWarning("Cannot pickup order: OrderId={OrderId}, Reason={Reason}", orderId, "No agent assigned");
                return PickupResult.NoAgentAssigned;
            }

            order.Status = OrderStatus.PickedUp;
            await _orderRepository.UpdateOrderAsync(order);

            // Fetch agent information
            var agent = await _agentServiceClient.GetAgentByIdAsync(order.AgentId.Value);
            var agentName = agent?.Name ?? string.Empty;

            _logger.LogAuditInformation(
                action: "OrderPickedUp",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: order.AgentId.Value,
                userRole: "Agent",
                message: "Order picked up: OrderId={OrderId}, CustomerId={CustomerId}, AgentId={AgentId}, AgentName={AgentName}",
                args: new object[] { order.Id, order.CustomerId, order.AgentId.Value, agentName });

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
            _logger.LogDebug("Published OrderPickedUpEvent to Kafka for OrderId: {OrderId}", order.Id);

            return PickupResult.Success;
        }

        /// <summary>
        /// Completes delivery, records metrics, and publishes delivery-completed events.
        /// </summary>
        public async Task<DeliveryResult> CompleteDeliveryAsync(int orderId)
        {
            _logger.LogInformation("Completing delivery: OrderId={OrderId}", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Cannot complete delivery: OrderId={OrderId}, Reason={Reason}", orderId, "Order not found");
                return DeliveryResult.OrderNotFound;
            }

            if (order.Status != OrderStatus.PickedUp)
            {
                _logger.LogWarning("Cannot complete delivery: OrderId={OrderId}, Reason={Reason}", orderId, $"Invalid status: {order.Status}");
                return DeliveryResult.InvalidStatus;
            }

            if (order.AgentId == null)
            {
                _logger.LogWarning("Cannot complete delivery: OrderId={OrderId}, Reason={Reason}", orderId, "No agent assigned");
                return DeliveryResult.NoAgentAssigned;
            }

            order.Status = OrderStatus.Delivered;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogAuditInformation(
                action: "OrderDelivered",
                resource: "Order",
                resourceId: order.Id.ToString(),
                userId: order.AgentId.Value,
                userRole: "Agent",
                message: "Order delivered: OrderId={OrderId}, CustomerId={CustomerId}, AgentId={AgentId}",
                args: new object[] { order.Id, order.CustomerId, order.AgentId.Value });

            // Publish OrderDeliveredEvent
            var orderEvent = new OrderDeliveredEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderDelivered, order.Id.ToString(), orderEvent);
            _logger.LogDebug("Published OrderDeliveredEvent to Kafka for OrderId: {OrderId}", order.Id);

            return DeliveryResult.Success;
        }

        /// <summary>
        /// Returns a customer's orders filtered by optional date range.
        /// </summary>
        public async Task<List<CustomerOrderResponse>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Getting order history for CustomerId: {CustomerId}, StartDate: {StartDate}, EndDate: {EndDate}", customerId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, startDate, endDate);

            _logger.LogInformation("Order history retrieved: CustomerId={CustomerId}, OrderCount={OrderCount}", customerId, orders.Count);

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

        /// <summary>
        /// Returns deliveries handled by an agent filtered by optional date range.
        /// </summary>
        public async Task<List<AgentDeliveryResponse>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Getting delivery history for AgentId: {AgentId}, StartDate: {StartDate}, EndDate: {EndDate}", agentId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByAgentIdAsync(agentId, startDate, endDate);

            _logger.LogInformation("Agent delivery history retrieved: AgentId={AgentId}, DeliveryCount={DeliveryCount}", agentId, orders.Count);

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

        /// <summary>
        /// Returns a partner's orders filtered by optional date range.
        /// </summary>
        public async Task<List<PartnerOrderResponse>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Getting order history for PartnerId: {PartnerId}, StartDate: {StartDate}, EndDate: {EndDate}", partnerId, startDate, endDate);

            var orders = await _orderRepository.GetOrdersByPartnerIdAsync(partnerId, startDate, endDate);

            _logger.LogInformation("Partner order history retrieved: PartnerId={PartnerId}, OrderCount={OrderCount}", partnerId, orders.Count);

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

        /// <summary>
        /// Retrieves detailed order info with authorization checks for the requesting user.
        /// </summary>
        public async Task<GetOrderDetailResult> GetOrderDetailAsync(int orderId, int userId, string userRole)
        {
            _logger.LogInformation("Getting order detail: OrderId={OrderId}, UserId={UserId}, Role={Role}", orderId, userId, userRole);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Order detail not found: OrderId={OrderId}", orderId);
                return new GetOrderDetailResult
                {
                    Success = false,
                    Error = GetOrderDetailError.NotFound
                };
            }

            // Check ownership based on user role
            var hasAccess = userRole switch
            {
                "Customer" => order.CustomerId == userId,
                "Partner" => order.PartnerId == userId,
                "Agent" => order.AgentId == userId,
                _ => false
            };

            if (!hasAccess)
            {
                _logger.LogAuditWarning(
                    action: "OrderAccessDenied",
                    resource: "Order",
                    resourceId: orderId.ToString(),
                    userId: userId,
                    userRole: userRole,
                    message: "Order access denied: OrderId={OrderId}, UserId={UserId}, Role={Role}",
                    args: new object[] { orderId, userId, userRole });
                return new GetOrderDetailResult
                {
                    Success = false,
                    Error = GetOrderDetailError.Forbidden
                };
            }

            _logger.LogInformation("Order detail retrieved: OrderId={OrderId}, UserId={UserId}, Role={Role}", orderId, userId, userRole);

            return new GetOrderDetailResult
            {
                Success = true,
                Order = new OrderDetailResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    PartnerId = order.PartnerId,
                    AgentId = order.AgentId,
                    DeliveryAddress = order.DeliveryAddress,
                    ServiceFee = order.ServiceFee,
                    DeliveryFee = order.DeliveryFee,
                    Status = order.Status.ToString(),
                    OrderCreatedTime = order.CreatedAt.ToString("O"),
                    Items = order.Items.Select(i => new OrderDetailItemResponse
                    {
                        FoodItemId = i.FoodItemId,
                        Name = i.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                }
            };
        }

        /// <summary>
        /// Lists active orders belonging to a customer.
        /// </summary>
        public async Task<List<CustomerOrderResponse>> GetActiveOrdersByCustomerIdAsync(int customerId)
        {
            _logger.LogInformation("Getting active orders for CustomerId: {CustomerId}", customerId);

            var orders = await _orderRepository.GetActiveOrdersByCustomerIdAsync(customerId);

            _logger.LogInformation("Active customer orders retrieved: CustomerId={CustomerId}, OrderCount={OrderCount}", customerId, orders.Count);

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

        /// <summary>
        /// Lists active orders belonging to a partner.
        /// </summary>
        public async Task<List<PartnerOrderResponse>> GetActiveOrdersByPartnerIdAsync(int partnerId)
        {
            _logger.LogInformation("Getting active orders for PartnerId: {PartnerId}", partnerId);

            var orders = await _orderRepository.GetActiveOrdersByPartnerIdAsync(partnerId);

            _logger.LogInformation("Active partner orders retrieved: PartnerId={PartnerId}, OrderCount={OrderCount}", partnerId, orders.Count);

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

        /// <summary>
        /// Lists active deliveries assigned to an agent.
        /// </summary>
        public async Task<List<AgentDeliveryResponse>> GetActiveOrdersByAgentIdAsync(int agentId)
        {
            _logger.LogInformation("Getting active orders for AgentId: {AgentId}", agentId);

            var orders = await _orderRepository.GetActiveOrdersByAgentIdAsync(agentId);

            _logger.LogInformation("Active agent orders retrieved: AgentId={AgentId}, OrderCount={OrderCount}", agentId, orders.Count);

            var results = new List<AgentDeliveryResponse>();

            foreach (var o in orders)
            {
                // Fetch partner information
                var partner = await _partnerServiceClient.GetPartnerByIdAsync(o.PartnerId);

                results.Add(new AgentDeliveryResponse
                {
                    Id = o.Id,
                    CustomerId = o.CustomerId,
                    PartnerId = o.PartnerId,
                    PartnerName = partner?.Name ?? "Unknown Partner",
                    PartnerAddress = partner?.Address ?? string.Empty,
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
                });
            }

            return results;
        }

        /// <summary>
        /// Returns open orders that are not yet assigned to an agent.
        /// </summary>
        public async Task<List<AvailableJobResponse>> GetAvailableOrdersAsync()
        {
            _logger.LogInformation("Getting available orders for delivery agents");

            var orders = await _orderRepository.GetAvailableOrdersAsync();

            _logger.LogInformation("Available orders retrieved: OrderCount={OrderCount}", orders.Count);

            var results = new List<AvailableJobResponse>();

            foreach (var order in orders)
            {
                // Fetch partner information
                var partner = await _partnerServiceClient.GetPartnerByIdAsync(order.PartnerId);

                results.Add(new AvailableJobResponse
                {
                    OrderId = order.Id,
                    PartnerName = partner?.Name ?? "Unknown",
                    PartnerAddress = partner?.Address ?? string.Empty,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryFee = order.DeliveryFee,
                    Distance = order.Distance,
                    EstimatedMinutes = order.EstimatedMinutes,
                    CreatedAt = order.CreatedAt.ToString("O"),
                    Items = order.Items.Select(i => new AvailableJobItemResponse
                    {
                        Name = i.Name,
                        Quantity = i.Quantity
                    }).ToList()
                });
            }

            return results;
        }
    }
}