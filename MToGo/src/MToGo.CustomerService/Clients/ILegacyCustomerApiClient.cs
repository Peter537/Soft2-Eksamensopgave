using MToGo.CustomerService.Models;

namespace MToGo.CustomerService.Clients;

public interface ILegacyCustomerApiClient
{
    Task<CreateCustomerResponse> CreateCustomerAsync(Customer request);
    Task<LegacyLoginResponse> LoginAsync(CustomerLoginRequest request);
    Task<CustomerProfileResponse> GetCustomerAsync(int id);
    Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request);
    Task DeleteCustomerAsync(int id);
}
