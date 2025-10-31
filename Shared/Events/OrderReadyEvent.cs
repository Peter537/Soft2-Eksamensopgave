namespace Shared.Events;

public class OrderReadyEvent
{
    public string OrderId { get; set; } = "";
    public DateTime ReadyAt { get; set; }
}
