using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Services;

public interface ICustomerService
{
    Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request);
    Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request);
    Task<CustomerProfileResponse> GetCustomerAsync(int id);
    Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request);
    Task DeleteCustomerAsync(int id);
}
