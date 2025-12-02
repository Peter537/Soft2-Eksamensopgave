namespace MToGo.Shared.WebSocket.Events.Agent
{
    public class OrderReadyEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public int AgentId { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }
}