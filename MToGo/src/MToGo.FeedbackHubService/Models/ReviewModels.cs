using System.ComponentModel.DataAnnotations;

namespace MToGo.FeedbackHubService.Models;

public class CreateReviewRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int PartnerId { get; set; }

    public int? AgentId { get; set; }

    [Range(0, 5, ErrorMessage = "Food rating must be between 0 and 5")]
    public int FoodRating { get; set; }

    [Range(0, 5, ErrorMessage = "Agent rating must be between 0 and 5")]
    public int AgentRating { get; set; }

    [Range(0, 5, ErrorMessage = "Order rating must be between 0 and 5")]
    public int OrderRating { get; set; }

    [MaxLength(500, ErrorMessage = "Food comment cannot exceed 500 characters")]
    public string? FoodComment { get; set; }

    [MaxLength(500, ErrorMessage = "Agent comment cannot exceed 500 characters")]
    public string? AgentComment { get; set; }

    [MaxLength(500, ErrorMessage = "Order comment cannot exceed 500 characters")]
    public string? OrderComment { get; set; }
}

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

// Query parameters for filtering reviews
public class ReviewFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Amount { get; set; }
}

// Response models matching the API specification
public class OrderReviewResponse
{
    public int? AgentId { get; set; }
    public int PartnerId { get; set; }
    public int CustomerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}

public class AgentReviewResponse
{
    public int OrderId { get; set; }
    public int PartnerId { get; set; }
    public int CustomerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}

public class CustomerReviewResponse
{
    public int OrderId { get; set; }
    public int? AgentId { get; set; }
    public int PartnerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}

public class PartnerReviewResponse
{
    public int OrderId { get; set; }
    public int? AgentId { get; set; }
    public int CustomerId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public RatingsDto Ratings { get; set; } = new();
    public CommentsDto Comments { get; set; } = new();
}

public class RatingsDto
{
    public int Food { get; set; }
    public int Agent { get; set; }
    public int Order { get; set; }
}

public class CommentsDto
{
    public string? Food { get; set; }
    public string? Agent { get; set; }
    public string? Order { get; set; }
}
