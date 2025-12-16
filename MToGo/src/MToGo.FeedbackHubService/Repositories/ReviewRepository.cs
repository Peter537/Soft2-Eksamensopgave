using Microsoft.EntityFrameworkCore;
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

public class ReviewRepository : IReviewRepository
{
    private readonly FeedbackHubDbContext _context;

    public ReviewRepository(FeedbackHubDbContext context)
    {
        _context = context;
    }

    public async Task<Review> CreateAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<Review?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    public async Task<bool> ExistsForOrderAsync(int orderId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.OrderId == orderId);
    }

    public async Task<IEnumerable<Review>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var query = _context.Reviews.AsQueryable();
        query = ApplyDateFilters(query, startDate, endDate);
        query = query.OrderByDescending(r => r.CreatedAt);
        
        if (amount.HasValue)
            query = query.Take(amount.Value);
        
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var query = _context.Reviews.Where(r => r.AgentId == agentId);
        query = ApplyDateFilters(query, startDate, endDate);
        query = query.OrderByDescending(r => r.CreatedAt);
        
        if (amount.HasValue)
            query = query.Take(amount.Value);
        
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var query = _context.Reviews.Where(r => r.CustomerId == customerId);
        query = ApplyDateFilters(query, startDate, endDate);
        query = query.OrderByDescending(r => r.CreatedAt);
        
        if (amount.HasValue)
            query = query.Take(amount.Value);
        
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null, int? amount = null)
    {
        var query = _context.Reviews.Where(r => r.PartnerId == partnerId);
        query = ApplyDateFilters(query, startDate, endDate);
        query = query.OrderByDescending(r => r.CreatedAt);
        
        if (amount.HasValue)
            query = query.Take(amount.Value);
        
        return await query.ToListAsync();
    }

    private static IQueryable<Review> ApplyDateFilters(IQueryable<Review> query, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var utcStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt >= utcStartDate);
        }
        
        if (endDate.HasValue)
        {
            // Add one day to include the entire end date (end of day)
            var utcEndDate = DateTime.SpecifyKind(endDate.Value.AddDays(1), DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt < utcEndDate);
        }
        
        return query;
    }
}
