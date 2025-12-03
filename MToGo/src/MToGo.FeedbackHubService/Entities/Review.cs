namespace MToGo.FeedbackHubService.Entities;

public class Review
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int PartnerId { get; set; }
    public int? AgentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Ratings (1-5)
    public int FoodRating { get; set; }
    public int AgentRating { get; set; }
    public int OrderRating { get; set; }

    // Comments (max 500 chars each)
    public string? FoodComment { get; set; }
    public string? AgentComment { get; set; }
    public string? OrderComment { get; set; }
}
