namespace MToGo.FeedbackHubService.Models;

public class AgentReviewResponse
{
    public int OrderId { get; set; }
    public int PartnerId { get; set; }
    public int CustomerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}
