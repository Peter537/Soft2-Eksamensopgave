namespace MToGo.Shared.Kafka.Events
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
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}