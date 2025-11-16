namespace OrderService.Models;

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
}
