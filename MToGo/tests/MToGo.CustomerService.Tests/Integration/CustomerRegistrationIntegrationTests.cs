using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;
using MToGo.Testing;

namespace MToGo.CustomerService.Tests.Integration;

public class CustomerRegistrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerRegistrationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMockedLegacyApi(Action<Mock<ILegacyCustomerApiClient>> setupMock, bool authenticated = false, string? userId = null, string? role = null)
    {
        var mockLegacyClient = new Mock<ILegacyCustomerApiClient>();
        setupMock(mockLegacyClient);

        if (authenticated)
        {
            TestAuthenticationHandler.SetTestUser(userId ?? "1", role ?? "Customer");
        }
        else
        {
            TestAuthenticationHandler.ClearTestUser();
        }

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ILegacyCustomerApiClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add mock
                services.AddSingleton(mockLegacyClient.Object);

                // Add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, options => { });
            });
        }).CreateClient();
    }

    #region US-1: Customer Registration Acceptance Criteria Tests

    [Fact]
    public async Task Register_WithValidDetails_CreatesNewAccount_Returns201()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
                .ReturnsAsync(new CreateCustomerResponse { Id = 1 });
        });

        var request = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            DeliveryAddress = "123 Main St, Copenhagen",
            NotificationMethod = "Email",
            Password = "SecurePass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
                .ThrowsAsync(new DuplicateEmailException("A customer with this email already exists."));
        });

        var request = new Customer
        {
            Name = "John Doe",
            Email = "existing@example.com",
            DeliveryAddress = "123 Main St",
            NotificationMethod = "Email",
            Password = "SecurePass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region US-2: Customer Login Acceptance Criteria Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
                .ReturnsAsync(new CustomerLoginResponse("valid-jwt-token"));
        });

        var request = new CustomerLoginRequest("john@example.com", "SecurePass123!");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerLoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));
        });

        var request = new CustomerLoginRequest("john@example.com", "WrongPassword!");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));
        });

        var request = new CustomerLoginRequest("nonexistent@example.com", "AnyPassword!");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Additional Registration Tests

    [Theory]
    [InlineData("Email")]
    [InlineData("Sms")]
    [InlineData("Push")]
    public async Task Register_WithDifferentNotificationMethods_Returns201Created(string notificationMethod)
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
                .ReturnsAsync(new CreateCustomerResponse { Id = 1 });
        });

        var request = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            DeliveryAddress = "123 Main St",
            NotificationMethod = notificationMethod,
            Password = "SecurePass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion

    #region US-3: Get Customer Profile Integration Tests

    [Fact]
    public async Task GetProfile_WithValidId_ReturnsCustomerProfile()
    {
        // Arrange
        var customerId = 1;
        var expectedProfile = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St, Copenhagen",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.GetCustomerAsync(customerId))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.GetAsync($"/api/v1/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("123 Main St, Copenhagen", result.DeliveryAddress);
        Assert.Equal("Email", result.NotificationMethod);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var customerId = 999;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.GetCustomerAsync(customerId))
                .ThrowsAsync(new KeyNotFoundException("Customer not found."));
        }, authenticated: true, userId: customerId.ToString(), role: "Management");

        // Act
        var response = await client.GetAsync($"/api/v1/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsLanguagePreference()
    {
        // Arrange
        var customerId = 1;
        var expectedProfile = new CustomerProfileResponse(
            Name: "Danish User",
            DeliveryAddress: "456 Strøget, Copenhagen",
            NotificationMethod: "Sms",
            PhoneNumber: "+4587654321",
            LanguagePreference: "da"
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.GetCustomerAsync(customerId))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.GetAsync($"/api/v1/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal("da", result.LanguagePreference);
    }

    #endregion

    #region US-3: Update Customer Profile Integration Tests

    [Fact]
    public async Task UpdateProfile_WithValidRequest_ReturnsUpdatedProfile()
    {
        // Arrange
        var customerId = 1;
        var updateRequest = new CustomerUpdateRequest(
            Name: "John Updated",
            DeliveryAddress: "789 New St, Copenhagen",
            NotificationMethod: "Push",
            PhoneNumber: "+4511223344",
            LanguagePreference: "da"
        );
        var expectedProfile = new CustomerProfileResponse(
            Name: "John Updated",
            DeliveryAddress: "789 New St, Copenhagen",
            NotificationMethod: "Push",
            PhoneNumber: "+4511223344",
            LanguagePreference: "da"
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal("John Updated", result.Name);
        Assert.Equal("789 New St, Copenhagen", result.DeliveryAddress);
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var customerId = 999;
        var updateRequest = new CustomerUpdateRequest(
            Name: "Test",
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
                .ThrowsAsync(new KeyNotFoundException("Customer not found."));
        }, authenticated: true, userId: customerId.ToString(), role: "Management");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithPartialUpdate_ReturnsUpdatedProfile()
    {
        // Arrange
        var customerId = 1;
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: "Updated Address Only",
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );
        var expectedProfile = new CustomerProfileResponse(
            Name: "Original Name",
            DeliveryAddress: "Updated Address Only",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal("Original Name", result.Name);
        Assert.Equal("Updated Address Only", result.DeliveryAddress);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("da")]
    public async Task UpdateProfile_WithDifferentLanguages_Returns200Ok(string language)
    {
        // Arrange
        var customerId = 1;
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: language
        );
        var expectedProfile = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: language
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(language, result.LanguagePreference);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Sms")]
    [InlineData("Push")]
    public async Task UpdateProfile_WithDifferentNotificationMethods_Returns200Ok(string notificationMethod)
    {
        // Arrange
        var customerId = 1;
        var updateRequest = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: notificationMethod,
            PhoneNumber: null,
            LanguagePreference: null
        );
        var expectedProfile = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St",
            NotificationMethod: notificationMethod,
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
                .ReturnsAsync(expectedProfile);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(notificationMethod, result.NotificationMethod);
    }

    #endregion

    #region US-18: Delete Customer Account Integration Tests

    [Fact]
    public async Task DeleteAccount_AuthenticatedCustomer_OwnAccount_Returns204NoContent()
    {
        // Arrange - Given authenticated
        var customerId = 1;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.DeleteCustomerAsync(customerId))
                .Returns(Task.CompletedTask);
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act - When confirming deletion
        var response = await client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Assert - Then account marked deleted (204 NoContent)
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_AuthenticatedCustomer_OtherAccount_Returns403Forbidden()
    {
        // Arrange - Customer trying to delete another customer's account
        var customerId = 1;
        var otherCustomerId = 2;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            // Should not be called
        }, authenticated: true, userId: customerId.ToString(), role: "Customer");

        // Act
        var response = await client.DeleteAsync($"/api/v1/customers/{otherCustomerId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_Management_AnyAccount_Returns204NoContent()
    {
        // Arrange - Management can delete any account
        var customerId = 5;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.DeleteCustomerAsync(customerId))
                .Returns(Task.CompletedTask);
        }, authenticated: true, userId: "999", role: "Management");

        // Act
        var response = await client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange - Given account deleted (or never existed)
        var customerId = 999;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.DeleteCustomerAsync(customerId))
                .ThrowsAsync(new KeyNotFoundException("Customer not found."));
        }, authenticated: true, userId: customerId.ToString(), role: "Management");

        // Act - When attempting to delete
        var response = await client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Assert - Then "Account not found" error (404)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange - Not authenticated
        var customerId = 1;

        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            // Should not be called
        }, authenticated: false);

        // Act
        var response = await client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterAccountDeleted_Returns401Unauthorized()
    {
        // Arrange - Given account deleted, When attempting login
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
                .ThrowsAsync(new UnauthorizedAccessException("Account not found."));
        });

        var request = new CustomerLoginRequest("deleted@example.com", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/customers/login", request);

        // Assert - Then "Account not found" error (401 Unauthorized)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
