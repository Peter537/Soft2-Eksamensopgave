using MToGo.FeedbackHubService.Entities;

namespace MToGo.FeedbackHubService.Repositories;

public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review);
    Task<Review?> GetByOrderIdAsync(int orderId);
    Task<bool> ExistsForOrderAsync(int orderId);
    Task<IEnumerable<Review>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<Review>> GetByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<Review>> GetByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
    Task<IEnumerable<Review>> GetByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null);
}
