namespace MToGo.Shared.Kafka.Events
{
    public class AgentAssignedEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public int AgentId { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }
}