using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Models;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.CustomerService.Services;

public interface ICustomerService
{
    Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request);
    Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request);
    Task<CustomerProfileResponse> GetCustomerAsync(int id);
    Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request);
    Task DeleteCustomerAsync(int id);
}

public class CustomerService : ICustomerService
{
    private readonly ILegacyCustomerApiClient _legacyApiClient;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public CustomerService(
        ILegacyCustomerApiClient legacyApiClient,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _legacyApiClient = legacyApiClient;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<CreateCustomerResponse> RegisterCustomerAsync(Customer request)
    {
        // Hash the password before sending to Legacy system
        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        var customerWithHashedPassword = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            DeliveryAddress = request.DeliveryAddress,
            NotificationMethod = request.NotificationMethod,
            Password = hashedPassword,
            PhoneNumber = request.PhoneNumber,
            LanguagePreference = request.LanguagePreference
        };
        
        return await _legacyApiClient.CreateCustomerAsync(customerWithHashedPassword);
    }

    public async Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        // Get customer data from Legacy system
        var legacyResponse = await _legacyApiClient.LoginAsync(request);
        
        // Verify password using the hashed password from Legacy
        if (!_passwordHasher.VerifyPassword(request.Password, legacyResponse.HashedPassword))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        
        // Generate JWT token using the shared JwtTokenService
        var jwt = _jwtTokenService.GenerateToken(
            userId: legacyResponse.Id,
            email: legacyResponse.Email,
            role: UserRoles.Customer,
            name: legacyResponse.Name
        );
        
        return new CustomerLoginResponse(jwt);
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
