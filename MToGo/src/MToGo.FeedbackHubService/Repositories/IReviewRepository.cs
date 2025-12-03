using MToGo.FeedbackHubService.Entities;

namespace MToGo.FeedbackHubService.Repositories;

public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review);
    Task<Review?> GetByOrderIdAsync(int orderId);
    Task<bool> ExistsForOrderAsync(int orderId);
}
