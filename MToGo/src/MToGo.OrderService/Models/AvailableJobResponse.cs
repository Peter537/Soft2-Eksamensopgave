namespace MToGo.OrderService.Models
{
    /// <summary>
    /// Response model for available delivery jobs (orders that have been accepted but not yet assigned to an agent).
    /// </summary>
    public class AvailableJobResponse
    {
        public int OrderId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public string Distance { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<AvailableJobItemResponse> Items { get; set; } = new();
    }

    public class AvailableJobItemResponse
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
