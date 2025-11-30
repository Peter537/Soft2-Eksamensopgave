using Microsoft.Extensions.Logging;

namespace MToGo.OrderService.Logging
{
    public static partial class LoggerMessages
    {
        // Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received CreateOrder request")]
        public static partial void ReceivedCreateOrderRequest(this ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Received AcceptOrder request for OrderId: {OrderId}")]
        public static partial void ReceivedAcceptOrderRequest(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "CreateOrder completed: OrderId={OrderId}")]
        public static partial void CreateOrderCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "AcceptOrder completed: OrderId={OrderId}")]
        public static partial void AcceptOrderCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "AcceptOrder failed: OrderId={OrderId}")]
        public static partial void AcceptOrderFailed(this ILogger logger, int orderId);

        // Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Creating order for CustomerId: {CustomerId}, PartnerId: {PartnerId}")]
        public static partial void CreatingOrder(this ILogger logger, int customerId, int partnerId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Calculated order total: {OrderTotal} DKK for {ItemCount} items")]
        public static partial void CalculatedOrderTotal(this ILogger logger, decimal orderTotal, int itemCount);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order created: OrderId={OrderId}, CustomerId={CustomerId}, PartnerId={PartnerId}, TotalAmount={TotalAmount} DKK")]
        public static partial void OrderCreated(this ILogger logger, int orderId, int customerId, int partnerId, decimal totalAmount);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderCreatedEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderCreatedEvent(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Accepting order: OrderId={OrderId}")]
        public static partial void AcceptingOrder(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot accept order: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotAcceptOrder(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order accepted: OrderId={OrderId}, CustomerId={CustomerId}")]
        public static partial void OrderAccepted(this ILogger logger, int orderId, int customerId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderAcceptedEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderAcceptedEvent(this ILogger logger, int orderId);

        // Reject Order - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received RejectOrder request for OrderId: {OrderId}")]
        public static partial void ReceivedRejectOrderRequest(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "RejectOrder completed: OrderId={OrderId}")]
        public static partial void RejectOrderCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "RejectOrder failed: OrderId={OrderId}")]
        public static partial void RejectOrderFailed(this ILogger logger, int orderId);

        // Reject Order - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Rejecting order: OrderId={OrderId}")]
        public static partial void RejectingOrder(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot reject order: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotRejectOrder(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order rejected: OrderId={OrderId}, CustomerId={CustomerId}, Reason={Reason}")]
        public static partial void OrderRejected(this ILogger logger, int orderId, int customerId, string reason);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderRejectedEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderRejectedEvent(this ILogger logger, int orderId);

        // Set Ready - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received SetReady request for OrderId: {OrderId}")]
        public static partial void ReceivedSetReadyRequest(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "SetReady completed: OrderId={OrderId}")]
        public static partial void SetReadyCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "SetReady failed: OrderId={OrderId}")]
        public static partial void SetReadyFailed(this ILogger logger, int orderId);

        // Set Ready - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Setting order ready: OrderId={OrderId}")]
        public static partial void SettingOrderReady(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot set order ready: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotSetOrderReady(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order set ready: OrderId={OrderId}, CustomerId={CustomerId}")]
        public static partial void OrderSetReady(this ILogger logger, int orderId, int customerId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderReadyEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderReadyEvent(this ILogger logger, int orderId);

        // Assign Agent - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received AssignAgent request for OrderId: {OrderId}, AgentId: {AgentId}")]
        public static partial void ReceivedAssignAgentRequest(this ILogger logger, int orderId, int agentId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "AssignAgent completed: OrderId={OrderId}, AgentId={AgentId}")]
        public static partial void AssignAgentCompleted(this ILogger logger, int orderId, int agentId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "AssignAgent failed: OrderId={OrderId}, AgentId={AgentId}")]
        public static partial void AssignAgentFailed(this ILogger logger, int orderId, int agentId);

        // Assign Agent - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Assigning agent to order: OrderId={OrderId}, AgentId={AgentId}")]
        public static partial void AssigningAgent(this ILogger logger, int orderId, int agentId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot assign agent to order: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotAssignAgent(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Agent assigned: OrderId={OrderId}, PartnerId={PartnerId}, AgentId={AgentId}")]
        public static partial void AgentAssigned(this ILogger logger, int orderId, int partnerId, int agentId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published AgentAssignedEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedAgentAssignedEvent(this ILogger logger, int orderId);

        // Pickup Order - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received PickupOrder request for OrderId: {OrderId}")]
        public static partial void ReceivedPickupOrderRequest(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "PickupOrder completed: OrderId={OrderId}")]
        public static partial void PickupOrderCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "PickupOrder failed: OrderId={OrderId}")]
        public static partial void PickupOrderFailed(this ILogger logger, int orderId);

        // Pickup Order - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Picking up order: OrderId={OrderId}")]
        public static partial void PickingUpOrder(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot pickup order: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotPickupOrder(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order picked up: OrderId={OrderId}, CustomerId={CustomerId}, AgentName={AgentName}")]
        public static partial void OrderPickedUp(this ILogger logger, int orderId, int customerId, string agentName);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderPickedUpEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderPickedUpEvent(this ILogger logger, int orderId);

        // Complete Delivery - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received CompleteDelivery request for OrderId: {OrderId}")]
        public static partial void ReceivedCompleteDeliveryRequest(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "CompleteDelivery completed: OrderId={OrderId}")]
        public static partial void CompleteDeliveryCompleted(this ILogger logger, int orderId);

        // Audit log
        [LoggerMessage(Level = LogLevel.Warning, Message = "CompleteDelivery failed: OrderId={OrderId}")]
        public static partial void CompleteDeliveryFailed(this ILogger logger, int orderId);

        // Complete Delivery - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Completing delivery: OrderId={OrderId}")]
        public static partial void CompletingDelivery(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot complete delivery: OrderId={OrderId}, Reason={Reason}")]
        public static partial void CannotCompleteDelivery(this ILogger logger, int orderId, string reason);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order delivered: OrderId={OrderId}, CustomerId={CustomerId}")]
        public static partial void OrderDelivered(this ILogger logger, int orderId, int customerId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Published OrderDeliveredEvent to Kafka for OrderId: {OrderId}")]
        public static partial void PublishedOrderDeliveredEvent(this ILogger logger, int orderId);

        // Get Customer Orders - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received GetCustomerOrders request for CustomerId: {CustomerId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void ReceivedGetCustomerOrdersRequest(this ILogger logger, int customerId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "GetCustomerOrders completed: CustomerId={CustomerId}, OrderCount={OrderCount}")]
        public static partial void GetCustomerOrdersCompleted(this ILogger logger, int customerId, int orderCount);

        // Get Customer Orders - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Getting order history for CustomerId: {CustomerId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void GettingOrderHistory(this ILogger logger, int customerId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order history retrieved: CustomerId={CustomerId}, OrderCount={OrderCount}")]
        public static partial void OrderHistoryRetrieved(this ILogger logger, int customerId, int orderCount);

        // Get Agent Deliveries - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received GetAgentDeliveries request for AgentId: {AgentId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void ReceivedGetAgentDeliveriesRequest(this ILogger logger, int agentId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "GetAgentDeliveries completed: AgentId={AgentId}, DeliveryCount={DeliveryCount}")]
        public static partial void GetAgentDeliveriesCompleted(this ILogger logger, int agentId, int deliveryCount);

        // Get Agent Deliveries - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Getting delivery history for AgentId: {AgentId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void GettingAgentDeliveryHistory(this ILogger logger, int agentId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Agent delivery history retrieved: AgentId={AgentId}, DeliveryCount={DeliveryCount}")]
        public static partial void AgentDeliveryHistoryRetrieved(this ILogger logger, int agentId, int deliveryCount);

        // Get Partner Orders - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received GetPartnerOrders request for PartnerId: {PartnerId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void ReceivedGetPartnerOrdersRequest(this ILogger logger, int partnerId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "GetPartnerOrders completed: PartnerId={PartnerId}, OrderCount={OrderCount}")]
        public static partial void GetPartnerOrdersCompleted(this ILogger logger, int partnerId, int orderCount);

        // Get Partner Orders - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Getting order history for PartnerId: {PartnerId}, StartDate: {StartDate}, EndDate: {EndDate}")]
        public static partial void GettingPartnerOrderHistory(this ILogger logger, int partnerId, DateTime? startDate, DateTime? endDate);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Partner order history retrieved: PartnerId={PartnerId}, OrderCount={OrderCount}")]
        public static partial void PartnerOrderHistoryRetrieved(this ILogger logger, int partnerId, int orderCount);

        // Get Order Detail - Controller logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Received GetOrderDetail request for OrderId: {OrderId}, UserId: {UserId}, Role: {Role}")]
        public static partial void ReceivedGetOrderDetailRequest(this ILogger logger, int orderId, int userId, string role);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "GetOrderDetail completed: OrderId={OrderId}")]
        public static partial void GetOrderDetailCompleted(this ILogger logger, int orderId);

        // Get Order Detail - Service logs
        [LoggerMessage(Level = LogLevel.Information, Message = "Getting order detail: OrderId={OrderId}, UserId={UserId}, Role={Role}")]
        public static partial void GettingOrderDetail(this ILogger logger, int orderId, int userId, string role);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Order detail not found: OrderId={OrderId}")]
        public static partial void OrderDetailNotFound(this ILogger logger, int orderId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Order detail access denied: OrderId={OrderId}, UserId={UserId}, Role={Role}")]
        public static partial void OrderDetailAccessDenied(this ILogger logger, int orderId, int userId, string role);

        // Audit log
        [LoggerMessage(Level = LogLevel.Information, Message = "Order detail retrieved: OrderId={OrderId}, UserId={UserId}, Role={Role}")]
        public static partial void OrderDetailRetrieved(this ILogger logger, int orderId, int userId, string role);
    }
}
