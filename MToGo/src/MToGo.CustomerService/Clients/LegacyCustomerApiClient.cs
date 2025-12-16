using System.Net;
using System.Net.Http.Json;
using MToGo.CustomerService.Exceptions;
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

    public async Task<LegacyLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        _logger.LogInformation("Attempting to log in");

        // Legacy API uses /post/login endpoint
        var response = await _httpClient.PostAsJsonAsync("/api/v1/legacy/customers/post/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Login failed - customer not found for email");
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LegacyLoginResponse>();
        
        _logger.LogInformation("Customer data retrieved for login verification");
        
        return result ?? throw new InvalidOperationException("Failed to deserialize login response.");
    }

    public async Task<CustomerProfileResponse> GetCustomerAsync(int id)
    {
        _logger.LogInformation("Getting customer with ID: {Id}", id);

        var response = await _httpClient.GetAsync($"/api/v1/legacy/customers/get/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found: {Id}", id);
            throw new KeyNotFoundException($"Customer with ID {id} not found.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        
        return result ?? throw new InvalidOperationException("Failed to deserialize customer response.");
    }

    public async Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request)
    {
        _logger.LogInformation("Updating customer with ID: {Id}", id);

        var response = await _httpClient.PatchAsJsonAsync($"/api/v1/legacy/customers/patch/{id}", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found: {Id}", id);
            throw new KeyNotFoundException($"Customer with ID {id} not found.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Update failed for customer {Id}: {Error}", id, error);
            throw new ArgumentException(error);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        
        _logger.LogInformation("Customer {Id} updated successfully", id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize update response.");
    }

    public async Task DeleteCustomerAsync(int id)
    {
        _logger.LogInformation("Deleting customer with ID: {Id}", id);

        var response = await _httpClient.DeleteAsync($"/api/v1/legacy/customers/delete/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer not found: {Id}", id);
            throw new KeyNotFoundException($"Customer with ID {id} not found.");
        }

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Customer {Id} deleted successfully", id);
    }
}
