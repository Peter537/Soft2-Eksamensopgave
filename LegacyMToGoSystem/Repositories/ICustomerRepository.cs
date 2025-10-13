using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);
}
