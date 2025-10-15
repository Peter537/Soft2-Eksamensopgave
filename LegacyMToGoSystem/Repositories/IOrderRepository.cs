using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
}
