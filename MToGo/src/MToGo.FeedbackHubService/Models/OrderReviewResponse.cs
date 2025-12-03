namespace MToGo.FeedbackHubService.Models;

public class OrderReviewResponse
{
    public int? AgentId { get; set; }
    public int PartnerId { get; set; }
    public int CustomerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}
