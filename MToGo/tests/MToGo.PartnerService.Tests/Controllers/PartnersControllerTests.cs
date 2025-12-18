using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.PartnerService.Controllers;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Services;

namespace MToGo.PartnerService.Tests.Controllers;

public class PartnersControllerTests
{
    private readonly Mock<IPartnerService> _mockPartnerService;
    private readonly Mock<ILogger<PartnersController>> _mockLogger;
    private readonly PartnersController _target;

    public PartnersControllerTests()
    {
        _mockPartnerService = new Mock<IPartnerService>();
        _mockLogger = new Mock<ILogger<PartnersController>>();
        _target = new PartnersController(_mockPartnerService.Object, _mockLogger.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };
        var expectedResponse = new CreatePartnerResponse { Id = 1 };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<CreatePartnerResponse>(createdResult.Value);
        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task Register_WithEmptyMenu_Returns400BadRequest()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>()
        };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ThrowsAsync(new EmptyMenuException("Menu cannot be empty. At least one menu item is required."));

        // Act
        var result = await _target.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "existing@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ThrowsAsync(new DuplicateEmailException("A partner with this email already exists."));

        // Act
        var result = await _target.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsCorrectLocationHeader()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };
        var expectedResponse = new CreatePartnerResponse { Id = 42 };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/v1/partners/42", createdResult.Location);
    }

    [Theory]
    [InlineData("Short Name", "Short St", "short@example.com", "Pass123!")]
    [InlineData("A Very Long Restaurant Name That Is Still Valid", "123 Long Street", "long.name@example.com", "LongPassword123!")]
    [InlineData("Restaurant With Numbers 123", "456 Number Ave", "numbers123@example.com", "Secure123!")]
    public async Task Register_WithVariousValidInputs_Returns201Created(string name, string address, string email, string password)
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = name,
            Address = address,
            Email = email,
            Password = password,
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Test Item", Price = 50.00m }
            }
        };
        var expectedResponse = new CreatePartnerResponse { Id = 1 };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task Register_WithMultipleMenuItems_Returns201Created()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m },
                new MenuItemRequest { Name = "Pepperoni Pizza", Price = 99.00m },
                new MenuItemRequest { Name = "Hawaiian Pizza", Price = 109.00m },
                new MenuItemRequest { Name = "Quattro Formaggi", Price = 119.00m }
            }
        };
        var expectedResponse = new CreatePartnerResponse { Id = 1 };

        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyMenuException_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>()
        };

        var expectedErrorMessage = "Menu cannot be empty. At least one menu item is required.";
        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ThrowsAsync(new EmptyMenuException(expectedErrorMessage));

        // Act
        var result = await _target.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorObject = badRequestResult.Value;
        
        // Use reflection to get the error property
        var errorProperty = errorObject?.GetType().GetProperty("error");
        var errorValue = errorProperty?.GetValue(errorObject) as string;
        
        Assert.Equal(expectedErrorMessage, errorValue);
    }

    [Fact]
    public async Task Register_DuplicateEmailException_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "existing@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };

        var expectedErrorMessage = "A partner with this email already exists.";
        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .ThrowsAsync(new DuplicateEmailException(expectedErrorMessage));

        // Act
        var result = await _target.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorObject = badRequestResult.Value;
        
        // Use reflection to get the error property
        var errorProperty = errorObject?.GetType().GetProperty("error");
        var errorValue = errorProperty?.GetValue(errorObject) as string;
        
        Assert.Equal(expectedErrorMessage, errorValue);
    }

    [Fact]
    public async Task Register_CallsServiceWithCorrectRequest()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };
        var expectedResponse = new CreatePartnerResponse { Id = 1 };

        PartnerRegisterRequest? capturedRequest = null;
        _mockPartnerService
            .Setup(x => x.RegisterPartnerAsync(It.IsAny<PartnerRegisterRequest>()))
            .Callback<PartnerRegisterRequest>(r => capturedRequest = r)
            .ReturnsAsync(expectedResponse);

        // Act
        await _target.Register(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Pizza Palace", capturedRequest.Name);
        Assert.Equal("123 Main Street", capturedRequest.Address);
        Assert.Equal("pizza@example.com", capturedRequest.Email);
        Assert.Equal("SecurePass123!", capturedRequest.Password);
        Assert.Single(capturedRequest.Menu);
        Assert.Equal("Margherita Pizza", capturedRequest.Menu[0].Name);
        Assert.Equal(89.00m, capturedRequest.Menu[0].Price);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "SecurePass123!"
        };
        var expectedResponse = new PartnerLoginResponse { Jwt = "jwt-token-123" };

        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<PartnerLoginResponse>(okResult.Value);
        Assert.Equal("jwt-token-123", response.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "WrongPassword!"
        };

        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException("Invalid email or password."));

        // Act
        var result = await _target.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };

        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException("Invalid email or password."));

        // Act
        var result = await _target.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "WrongPassword!"
        };

        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException("Invalid email or password."));

        // Act
        var result = await _target.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorObject = unauthorizedResult.Value;

        // Use reflection to get the error property
        var errorProperty = errorObject?.GetType().GetProperty("error");
        var errorValue = errorProperty?.GetValue(errorObject) as string;

        Assert.Equal("Invalid email or password.", errorValue);
    }

    [Fact]
    public async Task Login_CallsServiceWithCorrectRequest()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "SecurePass123!"
        };
        var expectedResponse = new PartnerLoginResponse { Jwt = "jwt-token" };

        PartnerLoginRequest? capturedRequest = null;
        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .Callback<PartnerLoginRequest>(r => capturedRequest = r)
            .ReturnsAsync(expectedResponse);

        // Act
        await _target.Login(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("pizza@example.com", capturedRequest.Email);
        Assert.Equal("SecurePass123!", capturedRequest.Password);
    }

    [Theory]
    [InlineData("partner1@example.com", "Password1!")]
    [InlineData("partner2@example.com", "Password2!")]
    [InlineData("partner.special@example.com", "Special123!")]
    public async Task Login_WithVariousValidCredentials_Returns200Ok(string email, string password)
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = email,
            Password = password
        };
        var expectedResponse = new PartnerLoginResponse { Jwt = "jwt-token-123" };

        _mockPartnerService
            .Setup(x => x.LoginAsync(It.IsAny<PartnerLoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion
}

