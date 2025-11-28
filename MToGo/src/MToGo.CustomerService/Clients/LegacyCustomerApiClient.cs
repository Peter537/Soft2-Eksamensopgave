using System.Net;
using System.Net.Http.Json;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Clients;

public class LegacyCustomerApiClient : ILegacyCustomerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegacyCustomerApiClient> _logger;

    public LegacyCustomerApiClient(HttpClient httpClient, ILogger<LegacyCustomerApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateCustomerResponse> CreateCustomerAsync(Customer request)
    {
        _logger.LogInformation("Creating customer");

        // Gateway routes /api/v1/legacy/customers/* to Legacy API
        // Legacy API uses /api/customers/post for creation
        var response = await _httpClient.PostAsJsonAsync("/api/v1/legacy/customers/post", request);

        // Handle duplicate email - Legacy API returns 400 for validation errors
        // or 500 for database constraint violations (unique email)
        if (response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.InternalServerError && 
                errorContent.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Duplicate email attempt (DB constraint)");
                throw new DuplicateEmailException("A customer with this email already exists.");
            }
            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Duplicate email attempt");
                throw new DuplicateEmailException("A customer with this email already exists.");
            }
            
            response.EnsureSuccessStatusCode();
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        
        _logger.LogInformation("Customer created successfully with ID: {Id}", result?.Id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize registration response.");
    }

    public async Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        _logger.LogInformation("Attempting to log in");

        // Legacy API uses /post/login endpoint
        var response = await _httpClient.PostAsJsonAsync("/api/v1/legacy/customers/post/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Login failed - invalid credentials for email");
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerLoginResponse>();
        
        _logger.LogInformation("Login successful");
        
        return result ?? throw new InvalidOperationException("Failed to deserialize login response.");
    }
}
