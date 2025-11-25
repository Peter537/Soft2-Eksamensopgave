using System.Collections.Generic;

namespace MToGo.Shared.WebSocket.Events.Partner
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public string OrderCreatedTime { get; set; }
        public List<OrderCreatedItem> Items { get; set; }

        public class OrderCreatedItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
        }
    }
}