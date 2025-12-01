using Moq;
using MToGo.AgentService.Entities;
using MToGo.AgentService.Exceptions;
using MToGo.AgentService.Models;
using MToGo.AgentService.Repositories;
using MToGo.AgentService.Services;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.AgentService.Tests.Services;

public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _mockAgentRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly AgentService.Services.AgentService _sut;

    public AgentServiceTests()
    {
        _mockAgentRepository = new Mock<IAgentRepository>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _sut = new AgentService.Services.AgentService(
            _mockAgentRepository.Object,
            _mockJwtTokenService.Object,
            _mockPasswordHasher.Object
        );
    }

    #region RegisterAgentAsync Tests

    [Fact]
    public async Task RegisterAgentAsync_WithValidRequest_ReturnsAgentId()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };

        _mockAgentRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed-password");

        _mockAgentRepository
            .Setup(x => x.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) =>
            {
                a.Id = 1;
                return a;
            });

        // Act
        var result = await _sut.RegisterAgentAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
        _mockAgentRepository.Verify(x => x.EmailExistsAsync(request.Email), Times.Once);
        _mockPasswordHasher.Verify(x => x.HashPassword(request.Password), Times.Once);
        _mockAgentRepository.Verify(x => x.CreateAsync(It.IsAny<Agent>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAgentAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "existing@example.com",
            Password = "SecurePass123!"
        };

        _mockAgentRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _sut.RegisterAgentAsync(request)
        );
        Assert.Equal("An agent with this email already exists.", exception.Message);

        _mockAgentRepository.Verify(x => x.CreateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAgentAsync_HashesPassword()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "PlainTextPassword!"
        };

        _mockAgentRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("$2a$12$hashedpasswordvalue");

        Agent? capturedAgent = null;
        _mockAgentRepository
            .Setup(x => x.CreateAsync(It.IsAny<Agent>()))
            .Callback<Agent>(a => capturedAgent = a)
            .ReturnsAsync((Agent a) =>
            {
                a.Id = 1;
                return a;
            });

        // Act
        await _sut.RegisterAgentAsync(request);

        // Assert
        Assert.NotNull(capturedAgent);
        Assert.Equal("$2a$12$hashedpasswordvalue", capturedAgent.Password);
        Assert.NotEqual("PlainTextPassword!", capturedAgent.Password);
    }

    [Fact]
    public async Task RegisterAgentAsync_SetsAgentAsActive()
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };

        _mockAgentRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        Agent? capturedAgent = null;
        _mockAgentRepository
            .Setup(x => x.CreateAsync(It.IsAny<Agent>()))
            .Callback<Agent>(a => capturedAgent = a)
            .ReturnsAsync((Agent a) =>
            {
                a.Id = 1;
                return a;
            });

        // Act
        await _sut.RegisterAgentAsync(request);

        // Assert
        Assert.NotNull(capturedAgent);
        Assert.True(capturedAgent.IsActive);
    }

    [Theory]
    [InlineData("Short", "s@e.com", "Pass1!")]
    [InlineData("A Very Long Name For An Agent", "verylongemail@verylongdomain.example.com", "VeryLongSecurePassword123!")]
    public async Task RegisterAgentAsync_WithVariousValidInputs_Succeeds(string name, string email, string password)
    {
        // Arrange
        var request = new AgentRegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };

        _mockAgentRepository
            .Setup(x => x.EmailExistsAsync(email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(password))
            .Returns("hashed");

        _mockAgentRepository
            .Setup(x => x.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) =>
            {
                a.Id = 1;
                return a;
            });

        // Act
        var result = await _sut.RegisterAgentAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsJwt()
    {
        // Arrange
        var request = new AgentLoginRequest
        {
            Email = "john.agent@example.com",
            Password = "SecurePass123!"
        };

        var agent = new Agent
        {
            Id = 1,
            Name = "John Agent",
            Email = request.Email,
            Password = "hashed-password",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(agent);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(request.Password, agent.Password))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(agent.Id, agent.Email, UserRoles.Agent, agent.Name))
            .Returns("valid-jwt-token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.Equal("valid-jwt-token", result.Jwt);
        _mockJwtTokenService.Verify(x => x.GenerateToken(agent.Id, agent.Email, UserRoles.Agent, agent.Name), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new AgentLoginRequest
        {
            Email = "john.agent@example.com",
            Password = "WrongPassword!"
        };

        var agent = new Agent
        {
            Id = 1,
            Name = "John Agent",
            Email = request.Email,
            Password = "hashed-password",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(agent);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(request.Password, agent.Password))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request)
        );

        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new AgentLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword!"
        };

        _mockAgentRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((Agent?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request)
        );

        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_DoesNotRevealWhetherEmailExists()
    {
        // Arrange
        var requestWithExistingEmail = new AgentLoginRequest
        {
            Email = "existing@example.com",
            Password = "WrongPassword!"
        };

        var requestWithNonExistentEmail = new AgentLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword!"
        };

        var agent = new Agent
        {
            Id = 1,
            Name = "John Agent",
            Email = "existing@example.com",
            Password = "hashed",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(agent);

        _mockAgentRepository
            .Setup(x => x.GetByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((Agent?)null);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act & Assert - Both should throw the same exception type with the same message
        var ex1 = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(requestWithExistingEmail)
        );

        var ex2 = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(requestWithNonExistentEmail)
        );

        Assert.Equal(ex1.Message, ex2.Message);
    }

    #endregion

    #region GetAgentAsync Tests

    [Fact]
    public async Task GetAgentAsync_WithValidId_ReturnsAgentProfile()
    {
        // Arrange
        var agentId = 1;
        var agent = new Agent
        {
            Id = agentId,
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "hashed",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync(agent);

        // Act
        var result = await _sut.GetAgentAsync(agentId);

        // Assert
        Assert.Equal(agentId, result.Id);
        Assert.Equal("John Agent", result.Name);
        Assert.Equal("john.agent@example.com", result.Email);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetAgentAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var agentId = 999;

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync((Agent?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetAgentAsync(agentId)
        );
    }

    [Fact]
    public async Task GetAgentAsync_DoesNotExposePassword()
    {
        // Arrange
        var agentId = 1;
        var agent = new Agent
        {
            Id = agentId,
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "super-secret-hashed-password",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync(agent);

        // Act
        var result = await _sut.GetAgentAsync(agentId);

        // Assert - The response type should not contain password
        var responseType = result.GetType();
        Assert.Null(responseType.GetProperty("Password"));
    }

    [Fact]
    public async Task GetAgentAsync_ReturnsCorrectActiveStatus()
    {
        // Arrange
        var agentId = 1;
        var inactiveAgent = new Agent
        {
            Id = agentId,
            Name = "Inactive Agent",
            Email = "inactive@example.com",
            Password = "hashed",
            IsActive = false
        };

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync(inactiveAgent);

        // Act
        var result = await _sut.GetAgentAsync(agentId);

        // Assert
        Assert.False(result.IsActive);
    }

    #endregion

    #region DeleteAgentAsync Tests

    [Fact]
    public async Task DeleteAgentAsync_WithValidId_DeletesAgent()
    {
        // Arrange
        var agentId = 1;
        var agent = new Agent
        {
            Id = agentId,
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "hashed",
            IsActive = true
        };

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync(agent);

        _mockAgentRepository
            .Setup(x => x.DeleteAsync(agentId))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAgentAsync(agentId);

        // Assert
        _mockAgentRepository.Verify(x => x.DeleteAsync(agentId), Times.Once);
    }

    [Fact]
    public async Task DeleteAgentAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var agentId = 999;

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .ReturnsAsync((Agent?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.DeleteAgentAsync(agentId)
        );

        _mockAgentRepository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAgentAsync_VerifiesAgentExistsBeforeDeleting()
    {
        // Arrange
        var agentId = 1;
        var agent = new Agent
        {
            Id = agentId,
            Name = "John Agent",
            Email = "john.agent@example.com",
            Password = "hashed",
            IsActive = true
        };

        var getByIdCalled = false;
        var deleteCalledAfterGet = false;

        _mockAgentRepository
            .Setup(x => x.GetByIdAsync(agentId))
            .Callback(() => getByIdCalled = true)
            .ReturnsAsync(agent);

        _mockAgentRepository
            .Setup(x => x.DeleteAsync(agentId))
            .Callback(() => deleteCalledAfterGet = getByIdCalled)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAgentAsync(agentId);

        // Assert
        Assert.True(deleteCalledAfterGet, "Delete should be called after verifying agent exists");
    }

    #endregion
}
