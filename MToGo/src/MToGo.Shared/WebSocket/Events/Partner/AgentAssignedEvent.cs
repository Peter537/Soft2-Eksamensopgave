namespace MToGo.Shared.WebSocket.Events.Partner
{
    public class AgentAssignedEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public int AgentId { get; set; }
        public string Timestamp { get; set; }
    }
}