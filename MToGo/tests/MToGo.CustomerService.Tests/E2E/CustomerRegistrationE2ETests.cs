using System.Net;
using System.Net.Http.Json;
using LegacyMToGo.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;
using MToGo.Testing;
using Testcontainers.PostgreSql;

namespace MToGo.CustomerService.Tests.E2E;

public class CustomerRegistrationE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private WebApplicationFactory<LegacyMToGo.Program> _legacyApiFactory = null!;
    private WebApplicationFactory<Program> _customerServiceFactory = null!;
    private HttpClient _customerServiceClient = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();
        await _postgresContainer.StartAsync();

        _legacyApiFactory = new WebApplicationFactory<LegacyMToGo.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LegacyContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LegacyContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));
                });
            });

        var legacyClient = _legacyApiFactory.CreateClient();

        _customerServiceFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ILegacyCustomerApiClient));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var httpClientDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IHttpClientFactory));
                    
                    services.AddSingleton<ILegacyCustomerApiClient>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<LegacyCustomerApiClientForE2E>>();
                        return new LegacyCustomerApiClientForE2E(legacyClient, logger);
                    });
                });
            });

        _customerServiceClient = _customerServiceFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _customerServiceClient?.Dispose();
        await _customerServiceFactory.DisposeAsync();
        await _legacyApiFactory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    #region US-1: Customer Registration E2E Tests

    [Fact]
    public async Task Register_WithValidDetails_CreatesCustomerInDatabase()
    {
        // Arrange
        var request = new Customer
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
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
        Assert.Equal("john.doe@example.com", customer.Email);
        Assert.Equal("123 Main St, Copenhagen", customer.DeliveryAddress);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        // Arrange - Create first customer
        var request = new Customer
        {
            Name = "First User",
            Email = "duplicate@example.com",
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
            Email = "duplicate@example.com", // Same email
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

    [Fact]
    public async Task Register_MultipleCustomers_AllCreatedSuccessfully()
    {
        // Arrange & Act
        var customers = new[]
        {
            new Customer
            {
                Name = "Customer1",
                Email = "customer1@example.com",
                DeliveryAddress = "Address 1",
                NotificationMethod = "Email",
                Password = "SecurePass123!",
                PhoneNumber = "+4511111111",
                LanguagePreference = "en"
            },
            new Customer
            {
                Name = "Customer2",
                Email = "customer2@example.com",
                DeliveryAddress = "Address 2",
                NotificationMethod = "Sms",
                Password = "SecurePass123!",
                PhoneNumber = "+4522222222",
                LanguagePreference = "da"
            },
            new Customer
            {
                Name = "Customer3",
                Email = "customer3@example.com",
                DeliveryAddress = "Address 3",
                NotificationMethod = "Push",
                Password = "SecurePass123!",
                PhoneNumber = "+4533333333",
                LanguagePreference = "en"
            }
        };

        var results = new List<HttpResponseMessage>();

        foreach (var customer in customers)
        {
            var response = await _customerServiceClient.PostAsJsonAsync("/api/v1/customers", customer);
            results.Add(response);
        }

        // Assert
        foreach (var result in results)
        {
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var content = await result.Content.ReadFromJsonAsync<CreateCustomerResponse>();
            Assert.NotNull(content);
            Assert.True(content.Id > 0);
        }
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

        // Call Legacy API directly (not through Gateway for E2E tests)
        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers/post", request);

        // Legacy API returns 400 for validation errors, but database constraint violations 
        // (duplicate email) result in 500 from EF Core's DbUpdateException
        if (response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogWarning("E2E: Duplicate email attempt or validation error: {Email}", request.Email);
            throw new MToGo.CustomerService.Exceptions.DuplicateEmailException(
                "A customer with this email already exists.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        
        _logger.LogInformation("E2E: Customer created successfully with ID: {Id}", result?.Id);
        
        return result ?? throw new InvalidOperationException("Failed to deserialize registration response.");
    }
}
