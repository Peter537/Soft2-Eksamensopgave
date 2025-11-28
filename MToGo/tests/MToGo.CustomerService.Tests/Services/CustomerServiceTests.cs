using Microsoft.Extensions.Logging;
using Moq;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Services;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ILegacyCustomerApiClient> _mockLegacyClient;
    private readonly Mock<ILogger<CustomerService.Services.CustomerService>> _mockLogger;
    private readonly CustomerService.Services.CustomerService _sut;

    public CustomerServiceTests()
    {
        _mockLegacyClient = new Mock<ILegacyCustomerApiClient>();
        _mockLogger = new Mock<ILogger<CustomerService.Services.CustomerService>>();
        _sut = new CustomerService.Services.CustomerService(_mockLegacyClient.Object, _mockLogger.Object);
    }

    #region RegisterCustomerAsync Tests

    [Fact]
    public async Task RegisterCustomerAsync_WithValidRequest_ReturnsCustomerId()
    {
        // Arrange
        var request = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            DeliveryAddress = "123 Main St",
            NotificationMethod = "Email",
            Password = "SecurePass123!",
            PhoneNumber = "+4512345678",
            LanguagePreference = "en"
        };
        var expectedResponse = new CreateCustomerResponse { Id = 1 };

        _mockLegacyClient
            .Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.RegisterCustomerAsync(request);

        // Assert
        Assert.Equal(expectedResponse.Id, result.Id);
        _mockLegacyClient.Verify(x => x.CreateCustomerAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task RegisterCustomerAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
    {
        // Arrange
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

        _mockLegacyClient
            .Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ThrowsAsync(new DuplicateEmailException("A customer with this email already exists."));

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _sut.RegisterCustomerAsync(request)
        );
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Sms")]
    [InlineData("Push")]
    public async Task RegisterCustomerAsync_WithDifferentNotificationMethods_Succeeds(string notificationMethod)
    {
        // Arrange
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
        var expectedResponse = new CreateCustomerResponse { Id = 1 };

        _mockLegacyClient
            .Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.RegisterCustomerAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
    }

    #endregion
}
