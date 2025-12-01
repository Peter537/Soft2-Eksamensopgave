using Moq;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Services;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.CustomerService.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ILegacyCustomerApiClient> _mockLegacyClient;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly CustomerService.Services.CustomerService _sut;

    public CustomerServiceTests()
    {
        _mockLegacyClient = new Mock<ILegacyCustomerApiClient>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _sut = new CustomerService.Services.CustomerService(
            _mockLegacyClient.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object);
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

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");

        _mockLegacyClient
            .Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.RegisterCustomerAsync(request);

        // Assert
        Assert.Equal(expectedResponse.Id, result.Id);
        _mockPasswordHasher.Verify(x => x.HashPassword("SecurePass123!"), Times.Once);
        _mockLegacyClient.Verify(x => x.CreateCustomerAsync(It.Is<Customer>(c => c.Password == "hashed-password")), Times.Once);
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

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");

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

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");

        _mockLegacyClient
            .Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.RegisterCustomerAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsJwt()
    {
        // Arrange
        var request = new CustomerLoginRequest("john@example.com", "SecurePass123!");
        var legacyResponse = new LegacyLoginResponse(1, "John Doe", "john@example.com", "hashed-password");
        var expectedJwt = "generated-jwt-token";

        _mockLegacyClient
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ReturnsAsync(legacyResponse);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword("SecurePass123!", "hashed-password"))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(1, "john@example.com", UserRoles.Customer, "John Doe"))
            .Returns(expectedJwt);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.Equal(expectedJwt, result.Jwt);
        _mockLegacyClient.Verify(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()), Times.Once);
        _mockPasswordHasher.Verify(x => x.VerifyPassword("SecurePass123!", "hashed-password"), Times.Once);
        _mockJwtTokenService.Verify(x => x.GenerateToken(1, "john@example.com", UserRoles.Customer, "John Doe"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new CustomerLoginRequest("john@example.com", "WrongPassword!");
        var legacyResponse = new LegacyLoginResponse(1, "John Doe", "john@example.com", "hashed-password");

        _mockLegacyClient
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ReturnsAsync(legacyResponse);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword("WrongPassword!", "hashed-password"))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request)
        );
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new CustomerLoginRequest("nonexistent@example.com", "AnyPassword!");

        _mockLegacyClient
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request)
        );
    }

    #endregion

    #region GetCustomerAsync Tests

    [Fact]
    public async Task GetCustomerAsync_WithValidId_ReturnsCustomerProfile()
    {
        // Arrange
        var customerId = 1;
        var expectedResponse = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        _mockLegacyClient
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetCustomerAsync(customerId);

        // Assert
        Assert.Equal(expectedResponse.Name, result.Name);
        Assert.Equal(expectedResponse.DeliveryAddress, result.DeliveryAddress);
        Assert.Equal(expectedResponse.NotificationMethod, result.NotificationMethod);
        _mockLegacyClient.Verify(x => x.GetCustomerAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var customerId = 999;

        _mockLegacyClient
            .Setup(x => x.GetCustomerAsync(customerId))
            .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetCustomerAsync(customerId)
        );
    }

    [Fact]
    public async Task GetCustomerAsync_ReturnsAllProfileFields()
    {
        // Arrange
        var customerId = 1;
        var expectedResponse = new CustomerProfileResponse(
            Name: "Jane Doe",
            DeliveryAddress: "456 Oak St, Copenhagen",
            NotificationMethod: "Sms",
            PhoneNumber: "+4587654321",
            LanguagePreference: "da"
        );

        _mockLegacyClient
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetCustomerAsync(customerId);

        // Assert
        Assert.Equal("Jane Doe", result.Name);
        Assert.Equal("456 Oak St, Copenhagen", result.DeliveryAddress);
        Assert.Equal("Sms", result.NotificationMethod);
        Assert.Equal("+4587654321", result.PhoneNumber);
        Assert.Equal("da", result.LanguagePreference);
    }

    #endregion

    #region UpdateCustomerAsync Tests

    [Fact]
    public async Task UpdateCustomerAsync_WithValidRequest_ReturnsUpdatedProfile()
    {
        // Arrange
        var customerId = 1;
        var request = new CustomerUpdateRequest(
            Name: "John Updated",
            DeliveryAddress: "789 New St",
            NotificationMethod: "Push",
            PhoneNumber: "+4511223344",
            LanguagePreference: "da"
        );
        var expectedResponse = new CustomerProfileResponse(
            Name: "John Updated",
            DeliveryAddress: "789 New St",
            NotificationMethod: "Push",
            PhoneNumber: "+4511223344",
            LanguagePreference: "da"
        );

        _mockLegacyClient
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateCustomerAsync(customerId, request);

        // Assert
        Assert.Equal(expectedResponse.Name, result.Name);
        Assert.Equal(expectedResponse.DeliveryAddress, result.DeliveryAddress);
        _mockLegacyClient.Verify(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var customerId = 999;
        var request = new CustomerUpdateRequest(
            Name: "Test",
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );

        _mockLegacyClient
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateCustomerAsync(customerId, request)
        );
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithPartialUpdate_ReturnsUpdatedProfile()
    {
        // Arrange
        var customerId = 1;
        var request = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: "Updated Address Only",
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: null
        );
        var expectedResponse = new CustomerProfileResponse(
            Name: "Original Name",
            DeliveryAddress: "Updated Address Only",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        _mockLegacyClient
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateCustomerAsync(customerId, request);

        // Assert
        Assert.Equal("Original Name", result.Name);
        Assert.Equal("Updated Address Only", result.DeliveryAddress);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Sms")]
    [InlineData("Push")]
    public async Task UpdateCustomerAsync_WithDifferentNotificationMethods_Succeeds(string notificationMethod)
    {
        // Arrange
        var customerId = 1;
        var request = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: notificationMethod,
            PhoneNumber: null,
            LanguagePreference: null
        );
        var expectedResponse = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St",
            NotificationMethod: notificationMethod,
            PhoneNumber: "+4512345678",
            LanguagePreference: "en"
        );

        _mockLegacyClient
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateCustomerAsync(customerId, request);

        // Assert
        Assert.Equal(notificationMethod, result.NotificationMethod);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("da")]
    public async Task UpdateCustomerAsync_WithDifferentLanguages_Succeeds(string language)
    {
        // Arrange
        var customerId = 1;
        var request = new CustomerUpdateRequest(
            Name: null,
            DeliveryAddress: null,
            NotificationMethod: null,
            PhoneNumber: null,
            LanguagePreference: language
        );
        var expectedResponse = new CustomerProfileResponse(
            Name: "John Doe",
            DeliveryAddress: "123 Main St",
            NotificationMethod: "Email",
            PhoneNumber: "+4512345678",
            LanguagePreference: language
        );

        _mockLegacyClient
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateCustomerAsync(customerId, request);

        // Assert
        Assert.Equal(language, result.LanguagePreference);
    }

    #endregion
}
