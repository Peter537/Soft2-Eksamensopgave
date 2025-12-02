using Microsoft.AspNetCore.Mvc;
using Moq;
using MToGo.AgentService.Controllers;
using MToGo.AgentService.Exceptions;
using MToGo.AgentService.Models;
using MToGo.AgentService.Services;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Context;

namespace MToGo.AgentService.Tests.Controllers;

public class AgentsControllerTests
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IUserContextAccessor> _mockUserContextAccessor;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly AgentsController _sut;

    public AgentsControllerTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockUserContextAccessor = new Mock<IUserContextAccessor>();
        _mockUserContext = new Mock<IUserContext>();

        // Default setup: Agent with ID 1 accessing their own data
        _mockUserContext.Setup(x => x.Id).Returns(1);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);
        _mockUserContextAccessor.Setup(x => x.UserContext).Returns(_mockUserContext.Object);

        _sut = new AgentsController(_mockAgentService.Object, _mockUserContextAccessor.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };
        var expectedResponse = new CreateAgentResponse { Id = 1 };

        _mockAgentService
            .Setup(x => x.RegisterAgentAsync(It.IsAny<AgentRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<CreateAgentResponse>(createdResult.Value);
        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "existing@example.com",
            Password = "SecurePass123!"
        };

        _mockAgentService
            .Setup(x => x.RegisterAgentAsync(It.IsAny<AgentRegisterRequest>()))
            .ThrowsAsync(new DuplicateEmailException("An agent with this email already exists."));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsCorrectLocationHeader()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };
        var expectedResponse = new CreateAgentResponse { Id = 42 };

        _mockAgentService
            .Setup(x => x.RegisterAgentAsync(It.IsAny<AgentRegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/v1/agents/42", createdResult.Location);
    }

    [Theory]
    [InlineData("Short Name", "short@example.com", "Pass123!")]
    [InlineData("A Very Long Agent Name That Is Still Valid", "long.name@example.com", "LongPassword123!")]
    [InlineData("Agent With Numbers 123", "agent123@example.com", "Secure123!")]
    public async Task Register_WithVariousValidInputs_Returns201Created(string name, string email, string password)
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };
        var expectedResponse = new CreateAgentResponse { Id = 1 };

        _mockAgentService
            .Setup(x => x.RegisterAgentAsync(It.IsAny<AgentRegisterRequest>()))
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
        var request = new AgentLoginRequest
        {
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };
        var expectedResponse = new AgentLoginResponse { Jwt = "valid-jwt-token" };

        _mockAgentService
            .Setup(x => x.LoginAsync(It.IsAny<AgentLoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<AgentLoginResponse>(okResult.Value);
        Assert.Equal("valid-jwt-token", response.Jwt);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new AgentLoginRequest
        {
            Email = "john.agent@example.com",
            Password = "WrongPassword!"
        };

        _mockAgentService
            .Setup(x => x.LoginAsync(It.IsAny<AgentLoginRequest>()))
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
        var request = new AgentLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword!"
        };

        _mockAgentService
            .Setup(x => x.LoginAsync(It.IsAny<AgentLoginRequest>()))
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
    public async Task GetProfile_AgentAccessingOwnProfile_Returns200Ok()
    {
        // Arrange
        var agentId = 1;
        var expectedResponse = new AgentProfileResponse
        {
            Id = agentId,
            Name = "John Agent",
            Email = "john.agent@example.com",
            IsActive = true
        };

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        _mockAgentService
            .Setup(x => x.GetAgentAsync(agentId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProfile(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<AgentProfileResponse>(okResult.Value);
        Assert.Equal("John Agent", response.Name);
        Assert.Equal("john.agent@example.com", response.Email);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetProfile_AgentAccessingOtherProfile_Returns403Forbid()
    {
        // Arrange
        var agentId = 1;
        var otherAgentId = 2;

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        // Act
        var result = await _sut.GetProfile(otherAgentId);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAgentService.Verify(x => x.GetAgentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetProfile_ManagementAccessingAnyProfile_Returns200Ok()
    {
        // Arrange
        var agentId = 5;
        var expectedResponse = new AgentProfileResponse
        {
            Id = agentId,
            Name = "Any Agent",
            Email = "any.agent@example.com",
            IsActive = true
        };

        _mockUserContext.Setup(x => x.Id).Returns(999);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.GetAgentAsync(agentId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProfile(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var agentId = 999;

        // Set up as Management so authorization passes
        _mockUserContext.Setup(x => x.Id).Returns(1);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.GetAgentAsync(agentId))
            .ThrowsAsync(new KeyNotFoundException("Agent not found."));

        // Act
        var result = await _sut.GetProfile(agentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsAllProfileFields()
    {
        // Arrange
        var agentId = 1;
        var expectedResponse = new AgentProfileResponse
        {
            Id = agentId,
            Name = "Complete Agent",
            Email = "complete.agent@example.com",
            IsActive = false
        };

        _mockAgentService
            .Setup(x => x.GetAgentAsync(agentId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProfile(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AgentProfileResponse>(okResult.Value);
        Assert.Equal(agentId, response.Id);
        Assert.Equal("Complete Agent", response.Name);
        Assert.Equal("complete.agent@example.com", response.Email);
        Assert.False(response.IsActive);
    }

    #endregion

    #region DeleteAccount Tests

    [Fact]
    public async Task DeleteAccount_AgentDeletingOwnAccount_Returns204NoContent()
    {
        // Arrange
        var agentId = 1;

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        _mockAgentService
            .Setup(x => x.DeleteAgentAsync(agentId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteAccount(agentId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
        _mockAgentService.Verify(x => x.DeleteAgentAsync(agentId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_AgentDeletingOtherAccount_Returns403Forbid()
    {
        // Arrange
        var agentId = 1;
        var otherAgentId = 2;

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        // Act
        var result = await _sut.DeleteAccount(otherAgentId);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAgentService.Verify(x => x.DeleteAgentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccount_ManagementDeletingAnyAccount_Returns204NoContent()
    {
        // Arrange
        var agentId = 5;

        _mockUserContext.Setup(x => x.Id).Returns(999);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.DeleteAgentAsync(agentId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteAccount(agentId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
        _mockAgentService.Verify(x => x.DeleteAgentAsync(agentId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var agentId = 999;

        // Set up as Management so authorization passes
        _mockUserContext.Setup(x => x.Id).Returns(1);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.DeleteAgentAsync(agentId))
            .ThrowsAsync(new KeyNotFoundException("Agent not found."));

        // Act
        var result = await _sut.DeleteAccount(agentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    #endregion

    #region UpdateActiveStatus Tests

    [Fact]
    public async Task SetActiveStatus_AgentUpdatingOwnStatus_Returns204NoContent()
    {
        // Arrange
        var agentId = 1;
        var request = new UpdateActiveStatusRequest { Active = true };

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        _mockAgentService
            .Setup(x => x.SetActiveStatusAsync(agentId, true))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SetActiveStatus(agentId, request);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
        _mockAgentService.Verify(x => x.SetActiveStatusAsync(agentId, true), Times.Once);
    }

    [Fact]
    public async Task SetActiveStatus_AgentUpdatingOtherAgentStatus_Returns403Forbid()
    {
        // Arrange
        var agentId = 1;
        var otherAgentId = 2;
        var request = new UpdateActiveStatusRequest { Active = true };

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        // Act
        var result = await _sut.SetActiveStatus(otherAgentId, request);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAgentService.Verify(x => x.SetActiveStatusAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task SetActiveStatus_ManagementUpdatingAnyAgentStatus_Returns204NoContent()
    {
        // Arrange
        var agentId = 5;
        var request = new UpdateActiveStatusRequest { Active = false };

        _mockUserContext.Setup(x => x.Id).Returns(999);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.SetActiveStatusAsync(agentId, false))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SetActiveStatus(agentId, request);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
        _mockAgentService.Verify(x => x.SetActiveStatusAsync(agentId, false), Times.Once);
    }

    [Fact]
    public async Task SetActiveStatus_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var agentId = 999;
        var request = new UpdateActiveStatusRequest { Active = true };

        // Set up as Management so authorization passes
        _mockUserContext.Setup(x => x.Id).Returns(1);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Management);

        _mockAgentService
            .Setup(x => x.SetActiveStatusAsync(agentId, true))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SetActiveStatus(agentId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task SetActiveStatus_ActivatesAgent_PassesCorrectValue()
    {
        // Arrange
        var agentId = 1;
        var request = new UpdateActiveStatusRequest { Active = true };

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        _mockAgentService
            .Setup(x => x.SetActiveStatusAsync(agentId, true))
            .ReturnsAsync(true);

        // Act
        await _sut.SetActiveStatus(agentId, request);

        // Assert
        _mockAgentService.Verify(x => x.SetActiveStatusAsync(agentId, true), Times.Once);
    }

    [Fact]
    public async Task SetActiveStatus_DeactivatesAgent_PassesCorrectValue()
    {
        // Arrange
        var agentId = 1;
        var request = new UpdateActiveStatusRequest { Active = false };

        _mockUserContext.Setup(x => x.Id).Returns(agentId);
        _mockUserContext.Setup(x => x.Role).Returns(UserRoles.Agent);

        _mockAgentService
            .Setup(x => x.SetActiveStatusAsync(agentId, false))
            .ReturnsAsync(true);

        // Act
        await _sut.SetActiveStatus(agentId, request);

        // Assert
        _mockAgentService.Verify(x => x.SetActiveStatusAsync(agentId, false), Times.Once);
    }

    #endregion
}
