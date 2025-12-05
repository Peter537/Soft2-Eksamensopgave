using System.Net;
using System.Net.Http.Json;
using LegacyMToGo.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.Testing;
using Testcontainers.PostgreSql;
using CustomerModel = MToGo.CustomerService.Models.Customer;

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
                        d => d.ServiceType == typeof(DbContextOptions<LegacyDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add test database
                    services.AddDbContext<LegacyDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));
                });
            });

        // Ensure database is created
        using (var scope = _legacyApiFactory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
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

                    // Add test authentication
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.AuthenticationScheme, options => { });
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

    private void SetupTestUser(string? userId = null, string role = "Customer")
    {
        TestAuthenticationHandler.SetTestUser(userId ?? "1", role);
    }

    private void SetupManagementUser()
    {
        TestAuthenticationHandler.SetTestUser("admin", "Management");
    }

    #region US-1: Customer Registration E2E Tests

    [Fact]
    public async Task Register_WithValidDetails_CreatesCustomerInDatabase()
    {
        // Arrange
        var uniqueEmail = $"john.doe.{Guid.NewGuid():N}@example.com";
        var request = new CustomerModel
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
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
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
        
        var request = new CustomerModel
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
        var duplicateRequest = new CustomerModel
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
        
        var registerRequest = new CustomerModel
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
        
        var registerRequest = new CustomerModel
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
        
        var registerRequest = new CustomerModel
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
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
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

    #region US-3: Get Customer Profile E2E Tests

    [Fact]
    public async Task GetProfile_AfterRegistration_ReturnsCustomerData()
    {
        // Arrange
        var uniqueEmail = $"getprofile.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Profile Test User",
            Email = uniqueEmail,
            DeliveryAddress = "456 Test St, Copenhagen",
            NotificationMethod = "Sms",
            Password = "TestPass123!",
            PhoneNumber = "+4599887766",
            LanguagePreference = "da"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act
        var getResponse = await _customerServiceClient.GetAsync($"/api/v1/customers/{registeredCustomer!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var profile = await getResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Profile Test User", profile.Name);
        Assert.Equal("456 Test St, Copenhagen", profile.DeliveryAddress);
        Assert.Equal("Sms", profile.NotificationMethod);
        Assert.Equal("+4599887766", profile.PhoneNumber);
        Assert.Equal("da", profile.LanguagePreference);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentId_Returns404()
    {
        // Setup authentication as management to access any profile
        SetupManagementUser();

        // Act
        var response = await _customerServiceClient.GetAsync("/api/v1/customers/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsLanguagePreferenceFromDatabase()
    {
        // Arrange - Create customer with Danish preference
        var uniqueEmail = $"langpref.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Danish User",
            Email = uniqueEmail,
            DeliveryAddress = "Strøget 123, København",
            NotificationMethod = "Email",
            Password = "DanskPass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "da"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act
        var getResponse = await _customerServiceClient.GetAsync($"/api/v1/customers/{registeredCustomer!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var profile = await getResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("da", profile.LanguagePreference);
    }

    #endregion

    #region US-3: Update Customer Profile E2E Tests

    [Fact]
    public async Task UpdateProfile_ChangesDeliveryAddress_PersistsInDatabase()
    {
        // Arrange - Create customer
        var uniqueEmail = $"updateaddr.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Update Test User",
            Email = uniqueEmail,
            DeliveryAddress = "Old Address 123",
            NotificationMethod = "Email",
            Password = "TestPass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act - Update delivery address
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: "New Address 456, Copenhagen",
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );
        var updateResponse = await _customerServiceClient.PatchAsJsonAsync(
            $"/api/v1/customers/{registeredCustomer!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("New Address 456, Copenhagen", updatedProfile.DeliveryAddress);

        // Verify in database
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
        var customer = await dbContext.Customers.FindAsync(registeredCustomer.Id);
        Assert.NotNull(customer);
        Assert.Equal("New Address 456, Copenhagen", customer.DeliveryAddress);
    }

    [Fact]
    public async Task UpdateProfile_ChangesLanguagePreference_PersistsInDatabase()
    {
        // Arrange - Create customer with English
        var uniqueEmail = $"updatelang.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Language Test User",
            Email = uniqueEmail,
            DeliveryAddress = "123 Test St",
            NotificationMethod = "Email",
            Password = "TestPass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act - Update to Danish
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: "da"
        );
        var updateResponse = await _customerServiceClient.PatchAsJsonAsync(
            $"/api/v1/customers/{registeredCustomer!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("da", updatedProfile.LanguagePreference);

        // Verify in database
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
        var customer = await dbContext.Customers.FindAsync(registeredCustomer.Id);
        Assert.NotNull(customer);
        Assert.Equal(LegacyMToGo.Entities.LanguagePreference.Da, customer.LanguagePreference);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNotificationMethod_PersistsInDatabase()
    {
        // Arrange
        var uniqueEmail = $"updatenotif.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Notification Test User",
            Email = uniqueEmail,
            DeliveryAddress = "123 Test St",
            NotificationMethod = "Email",
            Password = "TestPass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act - Update to SMS
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: "Sms",
            PhoneNumber: null,
            LanguagePreference: null
        );
        var updateResponse = await _customerServiceClient.PatchAsJsonAsync(
            $"/api/v1/customers/{registeredCustomer!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("Sms", updatedProfile.NotificationMethod);
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistentId_Returns404()
    {
        // Setup authentication as management to access any profile
        SetupManagementUser();

        // Arrange
        var updateRequest = new CustomerUpdateRequest(
            Name: "Test",
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );

        // Act
        var response = await _customerServiceClient.PatchAsJsonAsync("/api/v1/customers/99999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithFullUpdate_ChangesAllFields()
    {
        // Arrange
        var uniqueEmail = $"fullupdate.{Guid.NewGuid():N}@example.com";
        var registerRequest = new CustomerModel
        {
            Name = "Original Name",
            Email = uniqueEmail,
            DeliveryAddress = "Original Address",
            NotificationMethod = "Email",
            Password = "TestPass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        var registerResponse = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredCustomer = await registerResponse.Content.ReadFromJsonAsync<CreateCustomerResponse>();

        // Setup authentication for the registered customer
        SetupTestUser(registeredCustomer!.Id.ToString());

        // Act - Update all fields
        var updateRequest = new CustomerUpdateRequest(
            Name: "Updated Name",
            DeliveryAddress: "Updated Address 789",
            NotificationMethod: "Push",
            PhoneNumber: "+4598765432",
            LanguagePreference: "da"
        );
        var updateResponse = await _customerServiceClient.PatchAsJsonAsync(
            $"/api/v1/customers/{registeredCustomer!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("Updated Name", updatedProfile.Name);
        Assert.Equal("Updated Address 789", updatedProfile.DeliveryAddress);
        Assert.Equal("Push", updatedProfile.NotificationMethod);
        Assert.Equal("+4598765432", updatedProfile.PhoneNumber);
        Assert.Equal("da", updatedProfile.LanguagePreference);

        // Verify in database
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
        var customer = await dbContext.Customers.FindAsync(registeredCustomer.Id);
        Assert.NotNull(customer);
        Assert.Equal("Updated Name", customer.Name);
        Assert.Equal("Updated Address 789", customer.DeliveryAddress);
        Assert.Equal(LegacyMToGo.Entities.NotificationMethod.Push, customer.NotificationMethod);
        Assert.Equal("+4598765432", customer.PhoneNumber);
        Assert.Equal(LegacyMToGo.Entities.LanguagePreference.Da, customer.LanguagePreference);
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

    public async Task<CreateCustomerResponse> CreateCustomerAsync(CustomerModel request)
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

    public async Task<LegacyLoginResponse> LoginAsync(CustomerLoginRequest request)
    {
        _logger.LogInformation("E2E: Login attempt for: {Email}", request.Email);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers/post/login", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("E2E: Login failed for: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LegacyLoginResponse>();
        _logger.LogInformation("E2E: Login data retrieved for: {Email}", request.Email);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    public async Task<CustomerProfileResponse> GetCustomerAsync(int id)
    {
        _logger.LogInformation("E2E: Getting customer with ID: {Id}", id);

        var response = await _httpClient.GetAsync($"/api/v1/customers/get/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("E2E: Customer not found: {Id}", id);
            throw new KeyNotFoundException("Customer not found.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        _logger.LogInformation("E2E: Customer retrieved: {Id}", id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    public async Task<CustomerProfileResponse> UpdateCustomerAsync(int id, CustomerUpdateRequest request)
    {
        _logger.LogInformation("E2E: Updating customer with ID: {Id}", id);

        var response = await _httpClient.PatchAsJsonAsync($"/api/v1/customers/patch/{id}", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("E2E: Customer not found: {Id}", id);
            throw new KeyNotFoundException("Customer not found.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("E2E: Update failed - Status: {Status}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new ArgumentException(errorContent);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        _logger.LogInformation("E2E: Customer updated: {Id}", id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    public async Task DeleteCustomerAsync(int id)
    {
        _logger.LogInformation("E2E: Deleting customer with ID: {Id}", id);

        var response = await _httpClient.DeleteAsync($"/api/v1/customers/delete/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("E2E: Customer not found: {Id}", id);
            throw new KeyNotFoundException("Customer not found.");
        }

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("E2E: Customer deleted: {Id}", id);
    }
}
