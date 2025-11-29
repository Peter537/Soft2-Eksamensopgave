namespace MToGo.Shared.WebSocket.Events.Customer
{
    public class OrderAcceptedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}