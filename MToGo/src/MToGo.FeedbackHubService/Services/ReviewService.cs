using System.Text.RegularExpressions;
using MToGo.FeedbackHubService.Entities;
using MToGo.FeedbackHubService.Exceptions;
using MToGo.FeedbackHubService.Models;
using MToGo.FeedbackHubService.Repositories;

namespace MToGo.FeedbackHubService.Services;

public interface IReviewService
{
    /// <summary>
    /// Creates a review after validating ratings and comments.
    /// </summary>
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);
    /// <summary>
    /// Retrieves a review by order id if it exists.
    /// </summary>
    Task<ReviewResponse?> GetReviewByOrderIdAsync(int orderId);
    /// <summary>
    /// Checks if an order already has a review.
    /// </summary>
    Task<bool> HasReviewForOrderAsync(int orderId);
    /// <summary>
    /// Returns reviews across all orders with optional filters.
    /// </summary>
    Task<IEnumerable<OrderReviewResponse>> GetAllReviewsAsync(DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    /// <summary>
    /// Returns reviews for a specific agent with optional filters.
    /// </summary>
    Task<IEnumerable<AgentReviewResponse>> GetReviewsByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    /// <summary>
    /// Returns reviews for a specific customer with optional filters.
    /// </summary>
    Task<IEnumerable<CustomerReviewResponse>> GetReviewsByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    /// <summary>
    /// Returns reviews for a specific partner with optional filters.
    /// </summary>
    Task<IEnumerable<PartnerReviewResponse>> GetReviewsByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
}

public partial class ReviewService : IReviewService
{
    private readonly IReviewRepository _repository;

    public ReviewService(IReviewRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Validates ratings/comments, prevents duplicates, saves the review, and returns it.
    /// </summary>
    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        // Check if review already exists for this order
        if (await _repository.ExistsForOrderAsync(request.OrderId))
        {
            throw new DuplicateReviewException(request.OrderId);
        }

        // Require at least one rating
        if (request.FoodRating == 0 && request.AgentRating == 0 && request.OrderRating == 0)
        {
            throw new NoRatingsProvidedException();
        }

        // Validate ratings (0 is allowed = not rated, 1-5 are valid ratings)
        ValidateOptionalRating("Food", request.FoodRating);
        ValidateOptionalRating("Agent", request.AgentRating);
        ValidateOptionalRating("Order", request.OrderRating);

        // Sanitize and validate comments
        var foodComment = SanitizeComment(request.FoodComment);
        var agentComment = SanitizeComment(request.AgentComment);
        var orderComment = SanitizeComment(request.OrderComment);

        ValidateCommentLength("Food", foodComment);
        ValidateCommentLength("Agent", agentComment);
        ValidateCommentLength("Order", orderComment);

        var review = new Review
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            PartnerId = request.PartnerId,
            AgentId = request.AgentId,
            FoodRating = request.FoodRating,
            AgentRating = request.AgentRating,
            OrderRating = request.OrderRating,
            FoodComment = foodComment,
            AgentComment = agentComment,
            OrderComment = orderComment,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(review);
        return MapToResponse(created);
    }

    /// <summary>
    /// Looks up a review by order id and maps it to a response.
    /// </summary>
    public async Task<ReviewResponse?> GetReviewByOrderIdAsync(int orderId)
    {
        var review = await _repository.GetByOrderIdAsync(orderId);
        return review != null ? MapToResponse(review) : null;
    }

    /// <summary>
    /// Returns true if a review exists for the order.
    /// </summary>
    public async Task<bool> HasReviewForOrderAsync(int orderId)
    {
        return await _repository.ExistsForOrderAsync(orderId);
    }

    /// <summary>
    /// Retrieves all reviews within optional date/amount limits.
    /// </summary>
    public async Task<IEnumerable<OrderReviewResponse>> GetAllReviewsAsync(DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var reviews = await _repository.GetAllAsync(startDate, endDate, amount);
        return reviews.Select(MapToOrderReviewResponse);
    }

    /// <summary>
    /// Retrieves reviews for an agent within optional date/amount limits.
    /// </summary>
    public async Task<IEnumerable<AgentReviewResponse>> GetReviewsByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var reviews = await _repository.GetByAgentIdAsync(agentId, startDate, endDate, amount);
        return reviews.Select(MapToAgentReviewResponse);
    }

    /// <summary>
    /// Retrieves reviews for a customer within optional date/amount limits.
    /// </summary>
    public async Task<IEnumerable<CustomerReviewResponse>> GetReviewsByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var reviews = await _repository.GetByCustomerIdAsync(customerId, startDate, endDate, amount);
        return reviews.Select(MapToCustomerReviewResponse);
    }

    /// <summary>
    /// Retrieves reviews for a partner within optional date/amount limits.
    /// </summary>
    public async Task<IEnumerable<PartnerReviewResponse>> GetReviewsByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var reviews = await _repository.GetByPartnerIdAsync(partnerId, startDate, endDate, amount);
        return reviews.Select(MapToPartnerReviewResponse);
    }

    /// <summary>
    /// Ensures optional ratings are either 0 or between 1 and 5.
    /// </summary>
    private static void ValidateOptionalRating(string ratingType, int value)
    {
        // 0 means not rated (allowed), 1-5 are valid ratings
        if (value < 0 || value > 5)
        {
            throw new InvalidRatingException(ratingType, value);
        }
    }

    /// <summary>
    /// Enforces maximum length on comments.
    /// </summary>
    private static void ValidateCommentLength(string commentType, string? comment)
    {
        if (comment != null && comment.Length > 500)
        {
            throw new CommentTooLongException(commentType, comment.Length);
        }
    }

    /// <summary>
    /// Trims, strips HTML/control chars, and normalizes whitespace in comments.
    /// </summary>
    private static string? SanitizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        // Trim whitespace
        var sanitized = comment.Trim();

        // Remove potentially dangerous HTML/script tags
        sanitized = HtmlTagRegex().Replace(sanitized, string.Empty);

        // Remove null bytes and other control characters (except newlines and tabs)
        sanitized = ControlCharRegex().Replace(sanitized, string.Empty);

        // Normalize multiple spaces to single space
        sanitized = MultipleSpacesRegex().Replace(sanitized, " ");

        // If after sanitization the comment is empty, return null
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }

    private static ReviewResponse MapToResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            OrderId = review.OrderId,
            CustomerId = review.CustomerId,
            PartnerId = review.PartnerId,
            AgentId = review.AgentId,
            CreatedAt = review.CreatedAt,
            FoodRating = review.FoodRating,
            AgentRating = review.AgentRating,
            OrderRating = review.OrderRating,
            FoodComment = review.FoodComment,
            AgentComment = review.AgentComment,
            OrderComment = review.OrderComment
        };
    }

    private static OrderReviewResponse MapToOrderReviewResponse(Review review)
    {
        return new OrderReviewResponse
        {
            AgentId = review.AgentId,
            PartnerId = review.PartnerId,
            CustomerId = review.CustomerId,
            Timestamp = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Ratings = new RatingsDto
            {
                Food = review.FoodRating,
                Agent = review.AgentRating,
                Order = review.OrderRating
            },
            Comments = new CommentsDto
            {
                Food = review.FoodComment,
                Agent = review.AgentComment,
                Order = review.OrderComment
            }
        };
    }

    private static AgentReviewResponse MapToAgentReviewResponse(Review review)
    {
        return new AgentReviewResponse
        {
            OrderId = review.OrderId,
            PartnerId = review.PartnerId,
            CustomerId = review.CustomerId,
            Timestamp = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Ratings = new RatingsDto
            {
                Food = review.FoodRating,
                Agent = review.AgentRating,
                Order = review.OrderRating
            },
            Comments = new CommentsDto
            {
                Food = review.FoodComment,
                Agent = review.AgentComment,
                Order = review.OrderComment
            }
        };
    }

    private static CustomerReviewResponse MapToCustomerReviewResponse(Review review)
    {
        return new CustomerReviewResponse
        {
            OrderId = review.OrderId,
            AgentId = review.AgentId,
            PartnerId = review.PartnerId,
            Timestamp = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Ratings = new RatingsDto
            {
                Food = review.FoodRating,
                Agent = review.AgentRating,
                Order = review.OrderRating
            },
            Comments = new CommentsDto
            {
                Food = review.FoodComment,
                Agent = review.AgentComment,
                Order = review.OrderComment
            }
        };
    }

    private static PartnerReviewResponse MapToPartnerReviewResponse(Review review)
    {
        return new PartnerReviewResponse
        {
            OrderId = review.OrderId,
            AgentId = review.AgentId,
            CustomerId = review.CustomerId,
            Timestamp = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Ratings = new RatingsDto
            {
                Food = review.FoodRating,
                Agent = review.AgentRating,
                Order = review.OrderRating
            },
            Comments = new CommentsDto
            {
                Food = review.FoodComment,
                Agent = review.AgentComment,
                Order = review.OrderComment
            }
        };
    }

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled)]
    private static partial Regex ControlCharRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();
}
