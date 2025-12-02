namespace MToGo.Shared.WebSocket.Events.Agent
{
    public class OrderAcceptedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public List<OrderAcceptedItem> Items { get; set; } = new();

        public class OrderAcceptedItem
        {
            public int FoodItemId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}