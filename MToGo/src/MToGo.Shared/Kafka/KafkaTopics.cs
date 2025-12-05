namespace MToGo.Shared.Kafka
{
    public static class KafkaTopics
    {
        // Order lifecycle events
        public const string OrderCreated = "order-created";
        public const string OrderAccepted = "order-accepted";
        public const string OrderRejected = "order-rejected";
        public const string AgentAssigned = "agent-assigned";
        public const string OrderReady = "order-ready";
        public const string OrderPickedUp = "order-pickedup";
        public const string OrderDelivered = "order-delivered";

        // Centralized logging
        public const string AppLogs = "app-logs";
    }
}
