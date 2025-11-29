using Microsoft.AspNetCore.Mvc;
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
    private readonly CustomersController _sut;

    public CustomersControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _sut = new CustomersController(_mockCustomerService.Object);
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

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_WithValidId_Returns200Ok()
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

        _mockCustomerService
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProfile(customerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<CustomerProfileResponse>(okResult.Value);
        Assert.Equal("John Doe", response.Name);
        Assert.Equal("123 Main St", response.DeliveryAddress);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var customerId = 999;

        _mockCustomerService
            .Setup(x => x.GetCustomerAsync(customerId))
            .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        // Act
        var result = await _sut.GetProfile(customerId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsAllProfileFields()
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

        _mockCustomerService
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProfile(customerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomerProfileResponse>(okResult.Value);
        Assert.Equal("Jane Doe", response.Name);
        Assert.Equal("456 Oak St, Copenhagen", response.DeliveryAddress);
        Assert.Equal("Sms", response.NotificationMethod);
        Assert.Equal("+4587654321", response.PhoneNumber);
        Assert.Equal("da", response.LanguagePreference);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_WithValidRequest_Returns200Ok()
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

        _mockCustomerService
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateProfile(customerId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<CustomerProfileResponse>(okResult.Value);
        Assert.Equal("John Updated", response.Name);
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistentId_Returns404NotFound()
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

        _mockCustomerService
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        // Act
        var result = await _sut.UpdateProfile(customerId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidData_Returns400BadRequest()
    {
        // Arrange
        var customerId = 1;
        var request = new CustomerUpdateRequest(
            Name: "",
            DeliveryAddress: null,
            NotificationMethod: "InvalidMethod",
            PhoneNumber: null,
            LanguagePreference: null
        );

        _mockCustomerService
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ThrowsAsync(new ArgumentException("Invalid notification method."));

        // Act
        var result = await _sut.UpdateProfile(customerId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithPartialUpdate_Returns200Ok()
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

        _mockCustomerService
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateProfile(customerId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomerProfileResponse>(okResult.Value);
        Assert.Equal("Original Name", response.Name);
        Assert.Equal("Updated Address Only", response.DeliveryAddress);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("da")]
    public async Task UpdateProfile_WithDifferentLanguages_Returns200Ok(string language)
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

        _mockCustomerService
            .Setup(x => x.UpdateCustomerAsync(customerId, It.IsAny<CustomerUpdateRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateProfile(customerId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomerProfileResponse>(okResult.Value);
        Assert.Equal(language, response.LanguagePreference);
    }

    #endregion
}
