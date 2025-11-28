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
        var safeEmail = request.Email?.Replace("\r", "").Replace("\n", "");
        _logger.LogInformation("Registering new customer with email: {Email}", safeEmail);
        return await _legacyApiClient.CreateCustomerAsync(request);
    }
}
