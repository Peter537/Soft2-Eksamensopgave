using System;

namespace MToGo.FeedbackHubService.Models;

public class ReviewResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int PartnerId { get; set; }
    public int? AgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FoodRating { get; set; }
    public int AgentRating { get; set; }
    public int OrderRating { get; set; }
    public string? FoodComment { get; set; }
    public string? AgentComment { get; set; }
    public string? OrderComment { get; set; }
}
