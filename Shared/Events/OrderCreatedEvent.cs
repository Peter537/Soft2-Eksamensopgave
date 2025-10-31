namespace Shared.Events;

public class OrderCreatedEvent
{
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string OrderDetails { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}
