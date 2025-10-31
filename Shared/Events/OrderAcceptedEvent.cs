namespace Shared.Events;

public class OrderAcceptedEvent
{
    public string OrderId { get; set; } = "";
    public string RestaurantId { get; set; } = "";
    public DateTime AcceptedAt { get; set; }
}
