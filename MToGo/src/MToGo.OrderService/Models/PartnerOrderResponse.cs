namespace MToGo.OrderService.Models
{
    public class PartnerOrderResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? AgentId { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal ServiceFee { get; set; }
        public decimal DeliveryFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderCreatedTime { get; set; } = string.Empty;
        public List<PartnerOrderItemResponse> Items { get; set; } = new();
    }

    public class PartnerOrderItemResponse
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
