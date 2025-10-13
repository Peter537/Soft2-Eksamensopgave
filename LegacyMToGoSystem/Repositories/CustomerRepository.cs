using LegacyMToGoSystem.Infrastructure;
using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDatabase _database;
    private const string CollectionName = "customers";

    public CustomerRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        var customers = await LoadCustomersAsync();
        return customers.FirstOrDefault(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        var customers = await LoadCustomersAsync();
        return customers.FirstOrDefault(c => 
            c.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && !c.IsDeleted);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        var customers = await LoadCustomersAsync();
        return customers.Where(c => !c.IsDeleted).ToList();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var customers = await LoadCustomersAsync();
        
        customer.Id = customers.Any() ? customers.Max(c => c.Id) + 1 : 1;
        customer.CreatedAt = DateTime.UtcNow;
        customer.IsDeleted = false;
        
        customers.Add(customer);
        await SaveCustomersAsync(customers);
        
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        var customers = await LoadCustomersAsync();
        var index = customers.FindIndex(c => c.Id == customer.Id);
        
        if (index == -1)
        {
            throw new InvalidOperationException($"Customer with ID {customer.Id} not found");
        }

        customer.UpdatedAt = DateTime.UtcNow;
        customers[index] = customer;
        await SaveCustomersAsync(customers);
        
        return customer;
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var customers = await LoadCustomersAsync();
        var customer = customers.FirstOrDefault(c => c.Id == id);
        
        if (customer == null)
        {
            return false;
        }

        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        
        await SaveCustomersAsync(customers);
        return true;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var customers = await LoadCustomersAsync();
        return customers.Any(c => 
            c.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && !c.IsDeleted);
    }

    private async Task<List<Customer>> LoadCustomersAsync()
    {
        var customers = await _database.LoadAsync<List<Customer>>(CollectionName);
        return customers ?? new List<Customer>();
    }

    private async Task SaveCustomersAsync(List<Customer> customers)
    {
        await _database.SaveAsync(CollectionName, customers);
    }
}
