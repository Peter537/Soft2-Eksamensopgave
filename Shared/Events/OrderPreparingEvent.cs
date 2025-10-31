namespace Shared.Events;

public class OrderPreparingEvent
{
    public string OrderId { get; set; } = "";
    public DateTime StartedAt { get; set; }
}
