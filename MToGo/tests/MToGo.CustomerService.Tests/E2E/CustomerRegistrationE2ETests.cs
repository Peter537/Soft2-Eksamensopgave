using System.Net;
using System.Net.Http.Json;
using LegacyMToGo.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;
using Testcontainers.PostgreSql;

namespace MToGo.CustomerService.Tests.E2E;

public class CustomerRegistrationE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private WebApplicationFactory<LegacyMToGo.Program> _legacyApiFactory = null!;
    private WebApplicationFactory<Program> _customerServiceFactory = null!;
    private HttpClient _customerServiceClient = null!;
    private HttpClient _legacyClient = null!;

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("legacy_mtogo")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
            
        await _postgresContainer.StartAsync();

        // Setup Legacy API with test database
        _legacyApiFactory = new WebApplicationFactory<LegacyMToGo.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LegacyContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add test database
                    services.AddDbContext<LegacyContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));
                });
            });

        // Ensure database is created
        using (var scope = _legacyApiFactory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LegacyContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        _legacyClient = _legacyApiFactory.CreateClient();

        // Setup Customer Service with E2E client pointing to Legacy API
        _customerServiceFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing client registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ILegacyCustomerApiClient));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Remove HttpClient factory registrations for ILegacyCustomerApiClient
                    var httpClientDescriptors = services
                        .Where(d => d.ServiceType == typeof(IHttpClientFactory) ||
                                    d.ServiceType.FullName?.Contains("LegacyCustomerApiClient") == true)
                        .ToList();

                    foreach (var desc in httpClientDescriptors)
                    {
                        services.Remove(desc);
                    }

                    // Register our E2E client
                    services.AddSingleton<ILegacyCustomerApiClient>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<LegacyCustomerApiClientForE2E>>();
                        return new LegacyCustomerApiClientForE2E(_legacyClient, logger);
                    });
                });
            });

        _customerServiceClient = _customerServiceFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _customerServiceClient?.Dispose();
        _legacyClient?.Dispose();
        
        if (_customerServiceFactory != null)
            await _customerServiceFactory.DisposeAsync();
            
        if (_legacyApiFactory != null)
            await _legacyApiFactory.DisposeAsync();
            
        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
    }

    #region US-1: Customer Registration E2E Tests

    [Fact]
    public async Task Register_WithValidDetails_CreatesCustomerInDatabase()
    {
        // Arrange
        var uniqueEmail = $"john.doe.{Guid.NewGuid():N}@example.com";
        var request = new Customer
        {
            Name = "John Doe",
            Email = uniqueEmail,
            DeliveryAddress = "123 Main St, Copenhagen",
            NotificationMethod = "Email",
            Password = "SecurePass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        // Act
        var response = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        Assert.NotNull(result);
        Assert.True(result.Id > 0);

        // Verify customer exists in database
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyContext>();
        var customer = await dbContext.Customers.FindAsync(result.Id);
        
        Assert.NotNull(customer);
        Assert.Equal("John Doe", customer.Name);
        Assert.Equal(uniqueEmail, customer.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        // Arrange - Create first customer
        var uniqueEmail = $"duplicate.{Guid.NewGuid():N}@example.com";
        
        var request = new Customer
        {
            Name = "First User",
            Email = uniqueEmail,
            DeliveryAddress = "456 Oak St",
            NotificationMethod = "Sms",
            Password = "FirstPass123!",
            PhoneNumber = "+4587654321",
            LanguagePreference = "da"
        };

        var firstResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Try to create second customer with same email
        var duplicateRequest = new Customer
        {
            Name = "Second User",
            Email = uniqueEmail,
            DeliveryAddress = "789 Pine St",
            NotificationMethod = "Email",
            Password = "SecondPass123!",
            PhoneNumber = "+4511223344",
            LanguagePreference = "en"
        };

        var secondResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", duplicateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    #endregion

    #region US-2: Customer Login E2E Tests

    [Fact]
    public async Task Login_AfterSuccessfulRegistration_ReturnsJwtToken()
    {
        // Arrange
        var uniqueEmail = $"logintest.{Guid.NewGuid():N}@example.com";
        var password = "TestPass123!";
        
        var registerRequest = new Customer
        {
            Name = "Login Test User",
            Email = uniqueEmail,
            DeliveryAddress = "Test Address",
            NotificationMethod = "Email",
            Password = password,
            PhoneNumber = "+4599887766",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Act
        var loginRequest = new CustomerLoginRequest(uniqueEmail, password);
        var loginResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var result = await loginResponse.Content.ReadFromJsonAsync<CustomerLoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        // Arrange
        var uniqueEmail = $"invalidpass.{Guid.NewGuid():N}@example.com";
        
        var registerRequest = new Customer
        {
            Name = "Invalid Pass User",
            Email = uniqueEmail,
            DeliveryAddress = "Test Address",
            NotificationMethod = "Email",
            Password = "CorrectPass123!",
            PhoneNumber = "+4599887766",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Act
        var loginRequest = new CustomerLoginRequest(uniqueEmail, "WrongPass123!");
        var loginResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_VerifiesPasswordHashedWithBCrypt()
    {
        // Arrange
        var uniqueEmail = $"bcrypttest.{Guid.NewGuid():N}@example.com";
        var plainPassword = "PlainTextPass123!";
        
        var registerRequest = new Customer
        {
            Name = "BCrypt Test User",
            Email = uniqueEmail,
            DeliveryAddress = "Test Address",
            NotificationMethod = "Email",
            Password = plainPassword,
            PhoneNumber = "+4599887766",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Verify password is hashed in database
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyContext>();
        var customer = await dbContext.Customers.FindAsync(registeredCustomer!.Id);

        Assert.NotNull(customer);
        Assert.NotEqual(plainPassword, customer.Password);
        Assert.StartsWith("$2", customer.Password); // BCrypt hash prefix

        // Login should work with original password
        var loginRequest = new CustomerLoginRequest(uniqueEmail, plainPassword);
        var loginResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    #endregion
}

internal class LegacyCustomerApiClientForE2E : ILegacyCustomerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegacyCustomerApiClientForE2E> _logger;

    public LegacyCustomerApiClientForE2E(HttpClient httpClient, ILogger<LegacyCustomerApiClientForE2E> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateCustomerResponse> CreateCustomerAsync(Customer request)
    {
        _logger.LogInformation("E2E: Creating customer with email: {Email}", request.Email);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers/post", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("E2E: Create failed - Status: {Status}, Error: {Error}", 
                response.StatusCode, errorContent);

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new DuplicateEmailException("A customer with this email already exists.");
            }

            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        _logger.LogInformation("E2E: Customer created with ID: {Id}", result?.Id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    public async Task<CustomerLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        _logger.LogInformation("E2E: Login attempt for: {Email}", request.Email);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers/post/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("E2E: Login failed for: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerLoginResponse>();
        _logger.LogInformation("E2E: Login successful for: {Email}", request.Email);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }
}
