namespace PartnerService.Models;

public class Order
{
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string OrderDetails { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
}
