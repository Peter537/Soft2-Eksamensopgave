using Microsoft.EntityFrameworkCore;
using MToGo.FeedbackHubService.Data;
using MToGo.FeedbackHubService.Entities;

namespace MToGo.FeedbackHubService.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly FeedbackDbContext _context;

    public ReviewRepository(FeedbackDbContext context)
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
}
