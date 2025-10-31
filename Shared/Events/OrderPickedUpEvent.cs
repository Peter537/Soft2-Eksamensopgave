namespace Shared.Events;

public class OrderPickedUpEvent
{
    public string OrderId { get; set; } = "";
    public string DriverId { get; set; } = "";
    public DateTime PickedUpAt { get; set; }
}
