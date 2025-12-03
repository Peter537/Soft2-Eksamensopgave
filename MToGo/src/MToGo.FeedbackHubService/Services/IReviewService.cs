using MToGo.FeedbackHubService.Models;

namespace MToGo.FeedbackHubService.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);
    Task<ReviewResponse?> GetReviewByOrderIdAsync(int orderId);
    Task<bool> HasReviewForOrderAsync(int orderId);
}
