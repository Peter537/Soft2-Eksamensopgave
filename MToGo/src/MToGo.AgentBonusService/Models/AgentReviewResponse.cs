namespace MToGo.AgentBonusService.Models
{
    public class AgentReviewResponse
    {
        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public int CustomerId { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public ReviewRatings Ratings { get; set; } = new();
        public ReviewComments Comments { get; set; } = new();
    }

    public class ReviewRatings
    {
        public int Food { get; set; }
        public int Agent { get; set; }
        public int Order { get; set; }
    }

    public class ReviewComments
    {
        public string Food { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
        public string Order { get; set; } = string.Empty;
    }
}
