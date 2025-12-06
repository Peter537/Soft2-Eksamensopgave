namespace MToGo.WebSocketPartnerService.Models
{
    public class WebSocketMessage<T>
    {
        public string EventType { get; set; } = string.Empty;
        public T? Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
