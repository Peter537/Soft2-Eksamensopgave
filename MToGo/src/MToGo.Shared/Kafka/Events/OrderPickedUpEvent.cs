namespace MToGo.Shared.Kafka.Events
{
    public class OrderPickedUpEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public int CustomerId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}