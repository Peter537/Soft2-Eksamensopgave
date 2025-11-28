using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Tests.Integration;

public class CustomerRegistrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerRegistrationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMockedLegacyApi(Action<Mock<ILegacyCustomerApiClient>> setupMock)
    {
        var mockLegacyClient = new Mock<ILegacyCustomerApiClient>();
        setupMock(mockLegacyClient);

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
}
