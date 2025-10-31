namespace CentralHub.API.Models;

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public List<string> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
}
