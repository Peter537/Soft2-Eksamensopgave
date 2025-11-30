using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Services;

public class CustomerService : ICustomerService
{
    private readonly ILegacyCustomerApiClient _legacyApiClient;

    public CustomerService(ILegacyCustomerApiClient legacyApiClient)
    {
        _legacyApiClient = legacyApiClient;
    }

    public async Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request)
    {
        return await _legacyApiClient.CreateCustomerAsync(request);
    }

    public async Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        return await _legacyApiClient.LoginAsync(request);
    }

    public async Task<CustomerProfileResponse> GetCustomerAsync(int id)
    {
        return await _legacyApiClient.GetCustomerAsync(id);
    }

    public async Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request)
    {
        return await _legacyApiClient.UpdateCustomerAsync(id, request);
    }

    public async Task DeleteCustomerAsync(int id)
    {
        await _legacyApiClient.DeleteCustomerAsync(id);
    }
}
