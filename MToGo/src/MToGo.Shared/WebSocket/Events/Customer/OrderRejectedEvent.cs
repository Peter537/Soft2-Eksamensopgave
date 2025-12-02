namespace MToGo.Shared.WebSocket.Events.Customer
{
    public class OrderRejectedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}