using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Services;

public interface ICustomerService
{
    Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request);
    Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request);
}
