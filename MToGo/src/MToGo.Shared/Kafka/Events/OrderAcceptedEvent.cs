using System.Collections.Generic;

namespace MToGo.Shared.Kafka.Events
{
    public class OrderAcceptedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string PartnerName { get; set; }
        public string PartnerAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal DeliveryFee { get; set; }
        public string Timestamp { get; set; }
        public List<OrderAcceptedItem> Items { get; set; }

        public class OrderAcceptedItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
        }
    }
}