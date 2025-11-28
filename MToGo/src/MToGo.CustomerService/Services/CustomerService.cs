using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Services;

public class CustomerService : ICustomerService
{
    private readonly ILegacyCustomerApiClient _legacyApiClient;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ILegacyCustomerApiClient legacyApiClient, ILogger<CustomerService> logger)
    {
        _legacyApiClient = legacyApiClient;
        _logger = logger;
    }

    public async Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request)
    {
        _logger.LogInformation("Registering new customer");
        return await _legacyApiClient.CreateCustomerAsync(request);
    }

    public async Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        _logger.LogInformation("Processing login request");
        return await _legacyApiClient.LoginAsync(request);
    }

    public async Task<CustomerProfileResponse> GetCustomerAsync(int id)
    {
        _logger.LogInformation("Getting customer profile for ID: {Id}", id);
        return await _legacyApiClient.GetCustomerAsync(id);
    }

    public async Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request)
    {
        _logger.LogInformation("Updating customer profile for ID: {Id}", id);
        return await _legacyApiClient.UpdateCustomerAsync(id, request);
    }
}
