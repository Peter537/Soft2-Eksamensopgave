extern alias NotificationServiceApp;

using System.Net;
using System.Net.Http.Json;
using LegacyMToGo.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;
using MToGo.Testing;
using Testcontainers.PostgreSql;
using LegacyNotificationRequest = LegacyMToGo.Models.NotificationRequest;
using NotificationServiceProgram = NotificationServiceApp::Program;

namespace MToGo.NotificationService.Tests.E2E;

public class NotificationE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private WebApplicationFactory<LegacyMToGo.Program> _legacyApiFactory = null!;
    private WebApplicationFactory<NotificationServiceProgram> _notificationServiceFactory = null!;
    private HttpClient _notificationServiceClient = null!;
    private HttpClient _legacyClient = null!;

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer("legacy_mtogo");

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

        // Setup Notification Service with E2E client pointing to Legacy API
        _notificationServiceFactory = new WebApplicationFactory<NotificationServiceProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing client registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ILegacyNotificationApiClient));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Remove HttpClient factory registrations for ILegacyNotificationApiClient
                    var httpClientDescriptors = services
                        .Where(d => d.ServiceType == typeof(IHttpClientFactory) ||
                                    d.ServiceType.FullName?.Contains("LegacyNotificationApiClient") == true)
                        .ToList();

                    foreach (var desc in httpClientDescriptors)
                    {
                        services.Remove(desc);
                    }

                    // Register our E2E client
                    services.AddSingleton<ILegacyNotificationApiClient>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<LegacyNotificationApiClientForE2E>>();
                        return new LegacyNotificationApiClientForE2E(_legacyClient, logger);
                    });

                    // Add test authentication
                    services.AddTestAuthentication();
                });
            });

        _notificationServiceClient = _notificationServiceFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _notificationServiceClient?.Dispose();
        _legacyClient?.Dispose();

        if (_notificationServiceFactory != null)
            await _notificationServiceFactory.DisposeAsync();

        if (_legacyApiFactory != null)
            await _legacyApiFactory.DisposeAsync();

        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
    }

    private async Task<int> CreateTestCustomerAsync(string email, NotificationMethod notificationMethod = NotificationMethod.Email)
    {
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();

        var customer = new Customer
        {
            Name = "Test Customer",
            Email = email,
            DeliveryAddress = "123 Test St",
            NotificationMethod = notificationMethod,
            Password = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            PhoneNumber = "+4512345678",
            LanguagePreference = LanguagePreference.En
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return customer.Id;
    }

    #region US-42: Send Status Notifications E2E Tests

    [Fact]
    public async Task Notify_GivenStatusChange_SendsNotificationViaLegacy()
    {
        // Arrange - Create a customer in the database
        var uniqueEmail = $"notification.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Your order status has changed to: Preparing"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Notify_WithNonExistentCustomer_Returns404()
    {
        // Arrange - Use an ID that doesn't exist
        var request = new NotificationRequest
        {
            CustomerId = 99999,
            Message = "Test notification"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(NotificationMethod.Email)]
    [InlineData(NotificationMethod.Sms)]
    public async Task Notify_CustomerWithDifferentNotificationMethods_SendsSuccessfully(NotificationMethod method)
    {
        // Arrange - Create customer with specific notification method
        var uniqueEmail = $"notif.{method}.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail, method);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = $"Notification via {method}"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    #endregion

    #region Order Status Change Notification Tests

    [Fact]
    public async Task Notify_OrderPlaced_SendsNotificationToCustomer()
    {
        // Arrange
        var uniqueEmail = $"orderplaced.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Your order #12345 has been placed successfully!"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Notify_OrderPreparing_SendsNotificationToCustomer()
    {
        // Arrange
        var uniqueEmail = $"orderpreparing.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Your order #12345 is now being prepared!"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Notify_OrderOutForDelivery_SendsNotificationToCustomer()
    {
        // Arrange
        var uniqueEmail = $"orderdelivery.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Your order #12345 is out for delivery!"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Notify_OrderDelivered_SendsNotificationToCustomer()
    {
        // Arrange
        var uniqueEmail = $"orderdelivered.{Guid.NewGuid():N}@example.com";
        var customerId = await CreateTestCustomerAsync(uniqueEmail);

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Your order #12345 has been delivered. Enjoy your meal!"
        };

        // Act
        var response = await _notificationServiceClient.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    #endregion
}

internal class LegacyNotificationApiClientForE2E : ILegacyNotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegacyNotificationApiClientForE2E> _logger;

    public LegacyNotificationApiClientForE2E(HttpClient httpClient, ILogger<LegacyNotificationApiClientForE2E> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
    {
        _logger.LogInformation("E2E: Sending notification to customer {CustomerId}", request.CustomerId);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/notify", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("E2E: Customer {CustomerId} not found", request.CustomerId);
            throw new CustomerNotFoundException($"Customer with ID {request.CustomerId} not found.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("E2E: Failed to send notification. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);
            throw new NotificationFailedException($"Failed to send notification: {errorContent}");
        }

        _logger.LogInformation("E2E: Notification sent successfully to customer {CustomerId}", request.CustomerId);

        return new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };
    }
}

