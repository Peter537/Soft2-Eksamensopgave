using Microsoft.AspNetCore.Mvc;
using Moq;
using MToGo.ManagementService.Controllers;
using MToGo.ManagementService.Exceptions;
using MToGo.ManagementService.Models;
using MToGo.ManagementService.Services;

namespace MToGo.ManagementService.Tests.Controllers;

public class ManagementControllerTests
{
    private readonly Mock<IManagementService> _mockManagementService;
    private readonly ManagementController _sut;

    public ManagementControllerTests()
    {
        _mockManagementService = new Mock<IManagementService>();
        _sut = new ManagementController(_mockManagementService.Object);
    }

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };
        var expectedResponse = new ManagementLoginResponse { Jwt = "valid-jwt-token" };

        _mockManagementService
            .Setup(x => x.LoginAsync(It.IsAny<ManagementLoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ManagementLoginResponse>(okResult.Value);
        Assert.Equal("valid-jwt-token", response.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        _mockManagementService
            .Setup(x => x.LoginAsync(It.IsAny<ManagementLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException());

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_Returns401Unauthorized()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "wronguser",
            Password = "admin123"
        };

        _mockManagementService
            .Setup(x => x.LoginAsync(It.IsAny<ManagementLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException());

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsGenericErrorMessage_DoesNotRevealWhatWasWrong()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "wronguser",
            Password = "wrongpassword"
        };

        _mockManagementService
            .Setup(x => x.LoginAsync(It.IsAny<ManagementLoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException());

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var value = unauthorizedResult.Value;
        
        // Use reflection to get the anonymous type property
        var errorProperty = value?.GetType().GetProperty("error");
        var errorValue = errorProperty?.GetValue(value) as string;
        
        Assert.Equal("Invalid username or password.", errorValue);
    }

    [Fact]
    public async Task Login_CallsServiceWithCorrectRequest()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        ManagementLoginRequest? capturedRequest = null;
        _mockManagementService
            .Setup(x => x.LoginAsync(It.IsAny<ManagementLoginRequest>()))
            .Callback<ManagementLoginRequest>(r => capturedRequest = r)
            .ReturnsAsync(new ManagementLoginResponse { Jwt = "token" });

        // Act
        await _sut.Login(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("admin", capturedRequest.Username);
        Assert.Equal("admin123", capturedRequest.Password);
    }

    [Theory]
    [InlineData("admin", "admin123")]
    [InlineData("user", "password")]
    [InlineData("test@test.com", "Test123!")]
    public async Task Login_WithVariousInputs_PassesRequestToService(string username, string password)
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = username,
            Password = password
        };

        _mockManagementService
            .Setup(x => x.LoginAsync(It.Is<ManagementLoginRequest>(r => 
                r.Username == username && r.Password == password)))
            .ReturnsAsync(new ManagementLoginResponse { Jwt = "token" });

        // Act
        var result = await _sut.Login(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockManagementService.Verify(x => x.LoginAsync(It.Is<ManagementLoginRequest>(r => 
            r.Username == username && r.Password == password)), Times.Once);
    }

    #endregion
}
