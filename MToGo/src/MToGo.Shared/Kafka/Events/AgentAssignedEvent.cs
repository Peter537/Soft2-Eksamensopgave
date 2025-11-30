namespace MToGo.Shared.Kafka.Events
{
    public class AgentAssignedEvent
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public int AgentId { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        
        // Order details for the agent's personal view
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public List<AgentAssignedItem> Items { get; set; } = new();

        public class AgentAssignedItem
        {
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}