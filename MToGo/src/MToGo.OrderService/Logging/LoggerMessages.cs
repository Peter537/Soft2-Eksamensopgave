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
    }
}
