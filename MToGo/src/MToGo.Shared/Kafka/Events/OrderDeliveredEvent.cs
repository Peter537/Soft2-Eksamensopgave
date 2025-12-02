namespace MToGo.Shared.Kafka.Events
{
    public class OrderDeliveredEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }
}