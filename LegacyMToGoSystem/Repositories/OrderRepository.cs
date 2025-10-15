using LegacyMToGoSystem.Infrastructure;
using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDatabase _database;
    private const string CollectionName = "orders";

    public OrderRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        var orders = await LoadOrdersAsync();
        return orders.FirstOrDefault(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        var orders = await LoadOrdersAsync();
        return orders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.PlacedAt).ToList();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        var orders = await LoadOrdersAsync();
        
        order.Id = orders.Any() ? orders.Max(o => o.Id) + 1 : 1;
        order.PlacedAt = DateTime.UtcNow;
        
        orders.Add(order);
        await SaveOrdersAsync(orders);
        
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        var orders = await LoadOrdersAsync();
        var index = orders.FindIndex(o => o.Id == order.Id);
        
        if (index != -1)
        {
            orders[index] = order;
            await SaveOrdersAsync(orders);
        }
    }

    private async Task<List<Order>> LoadOrdersAsync()
    {
        var orders = await _database.LoadAsync<List<Order>>(CollectionName);
        return orders ?? new List<Order>();
    }

    private async Task SaveOrdersAsync(List<Order> orders)
    {
        await _database.SaveAsync(CollectionName, orders);
    }
}
