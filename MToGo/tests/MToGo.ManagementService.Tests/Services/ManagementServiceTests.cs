using Moq;
using MToGo.ManagementService.Entities;
using MToGo.ManagementService.Exceptions;
using MToGo.ManagementService.Models;
using MToGo.ManagementService.Repositories;
using MToGo.ManagementService.Services;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.ManagementService.Tests.Services;

public class ManagementServiceTests
{
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IManagementUserRepository> _mockRepository;
    private readonly ManagementService.Services.ManagementService _target;

    private readonly ManagementUser _testUser = new()
    {
        Id = 1,
        Username = "admin",
        Password = "hashed_admin123",
        Name = "Management Admin",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public ManagementServiceTests()
    {
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockRepository = new Mock<IManagementUserRepository>();

        // Setup password hasher to verify correctly
        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string password, string hash) => hash == $"hashed_{password}");

        _target = new ManagementService.Services.ManagementService(
            _mockRepository.Object,
            _mockJwtTokenService.Object,
            _mockPasswordHasher.Object
        );
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsJwt()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(1, "admin", UserRoles.Management, "Management Admin"))
            .Returns("valid-jwt-token");

        // Act
        var result = await _target.LoginAsync(request);

        // Assert
        Assert.Equal("valid-jwt-token", result.Jwt);
        _mockJwtTokenService.Verify(x => x.GenerateToken(1, "admin", UserRoles.Management, "Management Admin"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "wronguser",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("wronguser"))
            .ReturnsAsync((ManagementUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );

        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );

        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyUsername_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync(""))
            .ReturnsAsync((ManagementUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = ""
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );
    }

    [Fact]
    public async Task LoginAsync_UsernameIsCaseInsensitive()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "ADMIN",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(1, "admin", UserRoles.Management, "Management Admin"))
            .Returns("valid-jwt-token");

        // Act
        var result = await _target.LoginAsync(request);

        // Assert
        Assert.Equal("valid-jwt-token", result.Jwt);
    }

    [Fact]
    public async Task LoginAsync_PasswordIsCaseSensitive()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "ADMIN123" // Wrong case
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );
    }

    [Theory]
    [InlineData("admin", "admin123", true)]
    [InlineData("ADMIN", "admin123", true)]
    [InlineData("Admin", "admin123", true)]
    [InlineData("admin", "wrong", false)]
    [InlineData("wrong", "admin123", false)]
    [InlineData("", "", false)]
    public async Task LoginAsync_ValidatesCredentialsCorrectly(string username, string password, bool shouldSucceed)
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = username,
            Password = password
        };

        var normalizedUsername = username.ToLowerInvariant();
        
        if (normalizedUsername == "admin")
        {
            _mockRepository
                .Setup(x => x.GetByUsernameAsync("admin"))
                .ReturnsAsync(_testUser);
        }
        else
        {
            _mockRepository
                .Setup(x => x.GetByUsernameAsync(normalizedUsername))
                .ReturnsAsync((ManagementUser?)null);
        }

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(1, "admin", UserRoles.Management, "Management Admin"))
            .Returns("valid-jwt-token");

        // Act & Assert
        if (shouldSucceed)
        {
            var result = await _target.LoginAsync(request);
            Assert.NotNull(result);
            Assert.Equal("valid-jwt-token", result.Jwt);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _target.LoginAsync(request)
            );
        }
    }

    [Fact]
    public async Task LoginAsync_GeneratesTokenWithManagementRole()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUser);

        string? capturedRole = null;
        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, string, string, string>((id, email, role, name) => capturedRole = role)
            .Returns("token");

        // Act
        await _target.LoginAsync(request);

        // Assert
        Assert.Equal(UserRoles.Management, capturedRole);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        // Repository returns null for inactive users (filtered by IsActive)
        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync((ManagementUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _target.LoginAsync(request)
        );
    }

    [Fact]
    public async Task LoginAsync_UsesCorrectUserIdInToken()
    {
        // Arrange
        var userWithDifferentId = new ManagementUser
        {
            Id = 42,
            Username = "admin",
            Password = "hashed_admin123",
            Name = "Management Admin",
            IsActive = true
        };

        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(userWithDifferentId);

        int? capturedId = null;
        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, string, string, string>((id, email, role, name) => capturedId = id)
            .Returns("token");

        // Act
        await _target.LoginAsync(request);

        // Assert
        Assert.Equal(42, capturedId);
    }

    [Fact]
    public async Task LoginAsync_UsesCorrectNameInToken()
    {
        // Arrange
        var userWithCustomName = new ManagementUser
        {
            Id = 1,
            Username = "admin",
            Password = "hashed_admin123",
            Name = "Custom Manager Name",
            IsActive = true
        };

        var request = new ManagementLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        _mockRepository
            .Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(userWithCustomName);

        string? capturedName = null;
        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, string, string, string>((id, email, role, name) => capturedName = name)
            .Returns("token");

        // Act
        await _target.LoginAsync(request);

        // Assert
        Assert.Equal("Custom Manager Name", capturedName);
    }

    #endregion
}

