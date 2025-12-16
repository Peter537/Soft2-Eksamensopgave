namespace MToGo.AgentBonusService.Models
{
    public class BonusCalculationResponse
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public BonusPeriod Period { get; set; } = new();
        public bool Qualified { get; set; }
        public string? DisqualificationReason { get; set; }
        
        // Delivery stats
        public int DeliveryCount { get; set; }
        public int EarlyDeliveries { get; set; }
        public int NormalDeliveries { get; set; }
        public int LateDeliveries { get; set; }
        
        // Financial
        public decimal TotalDeliveryFees { get; set; }
        public decimal Contribution { get; set; }
        
        // Scores
        public decimal TimeScore { get; set; }
        public decimal ReviewScore { get; set; }
        public decimal Performance { get; set; }
        
        // Review stats
        public int ReviewCount { get; set; }
        public decimal AverageRating { get; set; }
        public bool UsedDefaultRating { get; set; }
        
        // Final result
        public decimal BonusAmount { get; set; }
        
        // Warnings
        public List<string> Warnings { get; set; } = new();
    }

    public class BonusPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
