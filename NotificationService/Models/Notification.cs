namespace NotificationService.Models
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OrderId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // OrderAccepted, OrderPreparing, OrderReady, OrderPickedUp, OrderDelivered
        public string Message { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
