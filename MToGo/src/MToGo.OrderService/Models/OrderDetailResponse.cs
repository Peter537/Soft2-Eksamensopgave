namespace MToGo.OrderService.Models
{
    public class OrderDetailResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int PartnerId { get; set; }
        public int? AgentId { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal ServiceFee { get; set; }
        public decimal DeliveryFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderCreatedTime { get; set; } = string.Empty;
        public List<OrderDetailItemResponse> Items { get; set; } = new();
    }

    public class OrderDetailItemResponse
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
