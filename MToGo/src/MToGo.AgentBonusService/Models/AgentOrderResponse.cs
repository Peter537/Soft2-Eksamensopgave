namespace MToGo.AgentBonusService.Models
{
    public class AgentOrderResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int PartnerId { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal ServiceFee { get; set; }
        public decimal DeliveryFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderCreatedTime { get; set; } = string.Empty;
        public List<AgentOrderItemResponse> Items { get; set; } = new();
    }

    public class AgentOrderItemResponse
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
