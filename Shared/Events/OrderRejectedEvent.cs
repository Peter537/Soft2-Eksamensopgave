namespace Shared.Events;

public class OrderRejectedEvent
{
    public string OrderId { get; set; } = "";
    public string RestaurantId { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime RejectedAt { get; set; }
}
