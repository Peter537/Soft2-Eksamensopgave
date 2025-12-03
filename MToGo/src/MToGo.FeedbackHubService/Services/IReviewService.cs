using MToGo.FeedbackHubService.Models;

namespace MToGo.FeedbackHubService.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);
    Task<ReviewResponse?> GetReviewByOrderIdAsync(int orderId);
    Task<bool> HasReviewForOrderAsync(int orderId);
    Task<IEnumerable<OrderReviewResponse>> GetAllReviewsAsync(DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<AgentReviewResponse>> GetReviewsByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<CustomerReviewResponse>> GetReviewsByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<PartnerReviewResponse>> GetReviewsByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
}
