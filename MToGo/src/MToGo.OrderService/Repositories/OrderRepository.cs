using MToGo.OrderService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MToGo.OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int id);
        Task UpdateOrderAsync(Order order);
        Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Order>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Order>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Order>> GetActiveOrdersByCustomerIdAsync(int customerId);
        Task<List<Order>> GetActiveOrdersByAgentIdAsync(int agentId);
        Task<List<Order>> GetActiveOrdersByPartnerIdAsync(int partnerId);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId);

            if (startDate.HasValue)
            {
                // Convert to UTC for PostgreSQL timestamp with time zone
                var utcStartDate = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= utcStartDate);
            }

            if (endDate.HasValue)
            {
                // Include the entire end day, convert to UTC
                var utcEndDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt < utcEndDate);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByAgentIdAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.AgentId == agentId);

            if (startDate.HasValue)
            {
                // Convert to UTC for PostgreSQL timestamp with time zone
                var utcStartDate = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= utcStartDate);
            }

            if (endDate.HasValue)
            {
                // Include the entire end day, convert to UTC
                var utcEndDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt < utcEndDate);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByPartnerIdAsync(int partnerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.PartnerId == partnerId);

            if (startDate.HasValue)
            {
                // Convert to UTC for PostgreSQL timestamp with time zone
                var utcStartDate = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= utcStartDate);
            }

            if (endDate.HasValue)
            {
                // Include the entire end day, convert to UTC
                var utcEndDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt < utcEndDate);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Rejected)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersByAgentIdAsync(int agentId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.AgentId == agentId && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Rejected)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersByPartnerIdAsync(int partnerId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.PartnerId == partnerId && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Rejected)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}