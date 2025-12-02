namespace MToGo.Shared.WebSocket.Events.Partner
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public string OrderCreatedTime { get; set; } = string.Empty;
        public List<OrderCreatedItem> Items { get; set; } = new();

        public class OrderCreatedItem
        {
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}