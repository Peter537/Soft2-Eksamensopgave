namespace Shared.Events;

public class OrderDeliveredEvent
{
    public string OrderId { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public DateTime DeliveredAt { get; set; }
}
