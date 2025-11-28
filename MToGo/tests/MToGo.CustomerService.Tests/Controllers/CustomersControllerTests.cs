using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.CustomerService.Controllers;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Services;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<ILogger<CustomersController>> _mockLogger;
    private readonly CustomersController _sut;

    public CustomersControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<CustomersController>>();
        _sut = new CustomersController(_mockCustomerService.Object, _mockLogger.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
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

        _mockCustomerService
            .Setup(x => x.RegisterCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<CreateCustomerResponse>(createdResult.Value);
        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
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

        _mockCustomerService
            .Setup(x => x.RegisterCustomerAsync(It.IsAny<Customer>()))
            .ThrowsAsync(new DuplicateEmailException("A customer with this email already exists."));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Sms")]
    [InlineData("Push")]
    public async Task Register_WithDifferentNotificationMethods_Returns201Created(string notificationMethod)
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

        _mockCustomerService
            .Setup(x => x.RegisterCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var request = new CustomerLoginRequest("john@example.com", "SecurePass123!");
        var expectedResponse = new CustomerLoginResponse("valid-jwt-token");

        _mockCustomerService
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<CustomerLoginResponse>(okResult.Value);
        Assert.Equal("valid-jwt-token", response.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new CustomerLoginRequest("john@example.com", "WrongPassword!");

        _mockCustomerService
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var request = new CustomerLoginRequest("nonexistent@example.com", "AnyPassword!");

        _mockCustomerService
            .Setup(x => x.LoginAsync(It.IsAny<CustomerLoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    #endregion
}
