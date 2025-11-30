using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Clients;

public interface ILegacyCustomerApiClient
{
    Task<CreateCustomerResponse> CreateCustomerAsync(Customer request);
    Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request);
    Task<CustomerProfileResponse> GetCustomerAsync(int id);
    Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request);
    Task DeleteCustomerAsync(int id);
}
