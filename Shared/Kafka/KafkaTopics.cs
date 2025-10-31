namespace Shared.Kafka;

public static class KafkaTopics
{
    public const string OrderCreated = "order-created";
    public const string OrderAccepted = "order-accepted";
    public const string OrderRejected = "order-rejected";
    public const string OrderPreparing = "order-preparing";
    public const string OrderReady = "order-ready";
    public const string OrderPickedUp = "order-pickedup";
    public const string OrderDelivered = "order-delivered";
    public const string DriverArriving = "driver-arriving";
    public const string LocationUpdate = "location-update";
}
