namespace MToGo.OrderService.Models
{
    public class OrderCreateRequest
    {
        public int CustomerId { get; set; }
        public int PartnerId { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public List<OrderCreateItem> Items { get; set; } = new();
    }

    public class OrderCreateItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}