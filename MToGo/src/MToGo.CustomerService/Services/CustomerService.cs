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
}
