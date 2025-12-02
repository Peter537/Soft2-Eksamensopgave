using Moq;
using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Repositories;
using MToGo.PartnerService.Services;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Password;

namespace MToGo.PartnerService.Tests.Services;

public class PartnerServiceTests
{
    private readonly Mock<IPartnerRepository> _mockPartnerRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly PartnerService.Services.PartnerService _sut;

    public PartnerServiceTests()
    {
        _mockPartnerRepository = new Mock<IPartnerRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();

        _sut = new PartnerService.Services.PartnerService(
            _mockPartnerRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object
        );
    }

    #region RegisterPartnerAsync Tests

    [Fact]
    public async Task RegisterPartnerAsync_WithValidRequest_ReturnsPartnerId()
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

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed-password");

        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        var result = await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
        _mockPartnerRepository.Verify(x => x.EmailExistsAsync(request.Email), Times.Once);
        _mockPasswordHasher.Verify(x => x.HashPassword(request.Password), Times.Once);
        _mockPartnerRepository.Verify(x => x.CreateAsync(It.IsAny<Partner>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPartnerAsync_WithEmptyMenu_ThrowsEmptyMenuException()
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmptyMenuException>(
            () => _sut.RegisterPartnerAsync(request)
        );
        Assert.Equal("Menu cannot be empty. At least one menu item is required.", exception.Message);

        _mockPartnerRepository.Verify(x => x.EmailExistsAsync(It.IsAny<string>()), Times.Never);
        _mockPartnerRepository.Verify(x => x.CreateAsync(It.IsAny<Partner>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPartnerAsync_WithNullMenu_ThrowsEmptyMenuException()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = null!
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmptyMenuException>(
            () => _sut.RegisterPartnerAsync(request)
        );
        Assert.Equal("Menu cannot be empty. At least one menu item is required.", exception.Message);

        _mockPartnerRepository.Verify(x => x.CreateAsync(It.IsAny<Partner>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPartnerAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
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

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _sut.RegisterPartnerAsync(request)
        );
        Assert.Equal("A partner with this email already exists.", exception.Message);

        _mockPartnerRepository.Verify(x => x.CreateAsync(It.IsAny<Partner>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPartnerAsync_HashesPassword()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "PlainTextPassword!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("$2a$12$hashedpasswordvalue");

        Partner? capturedPartner = null;
        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .Callback<Partner>(p => capturedPartner = p)
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.NotNull(capturedPartner);
        Assert.Equal("$2a$12$hashedpasswordvalue", capturedPartner.Password);
        Assert.NotEqual("PlainTextPassword!", capturedPartner.Password);
    }

    [Fact]
    public async Task RegisterPartnerAsync_SetsPartnerAsActive()
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

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        Partner? capturedPartner = null;
        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .Callback<Partner>(p => capturedPartner = p)
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.NotNull(capturedPartner);
        Assert.True(capturedPartner.IsActive);
    }

    [Fact]
    public async Task RegisterPartnerAsync_CreatesMenuItems()
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
                new MenuItemRequest { Name = "Hawaiian Pizza", Price = 109.00m }
            }
        };

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        Partner? capturedPartner = null;
        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .Callback<Partner>(p => capturedPartner = p)
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.NotNull(capturedPartner);
        Assert.Equal(3, capturedPartner.MenuItems.Count);
        
        var menuItems = capturedPartner.MenuItems.ToList();
        Assert.Equal("Margherita Pizza", menuItems[0].Name);
        Assert.Equal(89.00m, menuItems[0].Price);
        Assert.True(menuItems[0].IsActive);
        
        Assert.Equal("Pepperoni Pizza", menuItems[1].Name);
        Assert.Equal(99.00m, menuItems[1].Price);
        Assert.True(menuItems[1].IsActive);
        
        Assert.Equal("Hawaiian Pizza", menuItems[2].Name);
        Assert.Equal(109.00m, menuItems[2].Price);
        Assert.True(menuItems[2].IsActive);
    }

    [Fact]
    public async Task RegisterPartnerAsync_SetsMenuItemsAsActive()
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

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        Partner? capturedPartner = null;
        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .Callback<Partner>(p => capturedPartner = p)
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.NotNull(capturedPartner);
        Assert.All(capturedPartner.MenuItems, item => Assert.True(item.IsActive));
    }

    [Theory]
    [InlineData("Short", "A St", "s@e.com", "Pass123!")]
    [InlineData("A Very Long Restaurant Name That Is Still Valid", "A Very Long Address 123 Main Street Apartment 456", "long.name@example.com", "VeryLongSecurePassword123!")]
    public async Task RegisterPartnerAsync_WithVariousValidInputs_Succeeds(string name, string address, string email, string password)
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

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(password))
            .Returns("hashed");

        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        var result = await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task RegisterPartnerAsync_WithSingleMenuItem_Succeeds()
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
                new MenuItemRequest { Name = "Only Item", Price = 100.00m }
            }
        };

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        var result = await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task RegisterPartnerAsync_StoresCorrectPartnerData()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street, Copenhagen",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>
            {
                new MenuItemRequest { Name = "Margherita Pizza", Price = 89.00m }
            }
        };

        _mockPartnerRepository
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed");

        Partner? capturedPartner = null;
        _mockPartnerRepository
            .Setup(x => x.CreateAsync(It.IsAny<Partner>()))
            .Callback<Partner>(p => capturedPartner = p)
            .ReturnsAsync((Partner p) =>
            {
                p.Id = 1;
                return p;
            });

        // Act
        await _sut.RegisterPartnerAsync(request);

        // Assert
        Assert.NotNull(capturedPartner);
        Assert.Equal("Pizza Palace", capturedPartner.Name);
        Assert.Equal("123 Main Street, Copenhagen", capturedPartner.Address);
        Assert.Equal("pizza@example.com", capturedPartner.Email);
    }

    [Fact]
    public async Task RegisterPartnerAsync_ValidatesMenuBeforeCheckingEmail()
    {
        // Arrange
        var request = new PartnerRegisterRequest
        {
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "SecurePass123!",
            Menu = new List<MenuItemRequest>() // Empty menu
        };

        // Act & Assert
        await Assert.ThrowsAsync<EmptyMenuException>(
            () => _sut.RegisterPartnerAsync(request)
        );

        // Email check should never be called if menu validation fails first
        _mockPartnerRepository.Verify(x => x.EmailExistsAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "SecurePass123!"
        };

        var partner = new Partner
        {
            Id = 1,
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "$2a$12$hashedpassword",
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(request.Password, partner.Password))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(partner.Id, partner.Email, "Partner", partner.Name))
            .Returns("jwt-token-123");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt-token-123", result.Jwt);
        _mockPartnerRepository.Verify(x => x.GetByEmailAsync(request.Email), Times.Once);
        _mockPasswordHasher.Verify(x => x.VerifyPassword(request.Password, partner.Password), Times.Once);
        _mockJwtTokenService.Verify(x => x.GenerateToken(partner.Id, partner.Email, "Partner", partner.Name), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SecurePass123!"
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((Partner?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(request)
        );
        Assert.Equal("Invalid email or password.", exception.Message);

        _mockPasswordHasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "WrongPassword123!"
        };

        var partner = new Partner
        {
            Id = 1,
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "$2a$12$hashedpassword",
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(request.Password, partner.Password))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(request)
        );
        Assert.Equal("Invalid email or password.", exception.Message);

        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_VerifiesPasswordWithHash_NotPlainText()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "SecurePass123!"
        };

        var hashedPassword = "$2a$12$hashedpasswordvalue";
        var partner = new Partner
        {
            Id = 1,
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = hashedPassword,
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(request.Password, hashedPassword))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        await _sut.LoginAsync(request);

        // Assert - verify that VerifyPassword is called with the plain text password and the hashed password
        _mockPasswordHasher.Verify(x => x.VerifyPassword("SecurePass123!", "$2a$12$hashedpasswordvalue"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_GeneratesTokenWithCorrectRole()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "pizza@example.com",
            Password = "SecurePass123!"
        };

        var partner = new Partner
        {
            Id = 5,
            Name = "Pizza Palace",
            Address = "123 Main Street",
            Email = "pizza@example.com",
            Password = "$2a$12$hashedpassword",
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        await _sut.LoginAsync(request);

        // Assert - verify the role is "Partner"
        _mockJwtTokenService.Verify(x => x.GenerateToken(5, "pizza@example.com", "Partner", "Pizza Palace"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_GeneratesTokenWithCorrectUserData()
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = "burgerjoint@example.com",
            Password = "BurgerPass123!"
        };

        var partner = new Partner
        {
            Id = 42,
            Name = "Burger Joint",
            Address = "456 Oak Street",
            Email = "burgerjoint@example.com",
            Password = "$2a$12$hashedpassword",
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        int? capturedUserId = null;
        string? capturedEmail = null;
        string? capturedRole = null;
        string? capturedName = null;

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, string, string, string?>((id, email, role, name) =>
            {
                capturedUserId = id;
                capturedEmail = email;
                capturedRole = role;
                capturedName = name;
            })
            .Returns("jwt-token");

        // Act
        await _sut.LoginAsync(request);

        // Assert
        Assert.Equal(42, capturedUserId);
        Assert.Equal("burgerjoint@example.com", capturedEmail);
        Assert.Equal("Partner", capturedRole);
        Assert.Equal("Burger Joint", capturedName);
    }

    [Theory]
    [InlineData("partner1@example.com", "Password1!")]
    [InlineData("partner2@example.com", "Password2!")]
    [InlineData("partner.special@example.com", "Special123!")]
    public async Task LoginAsync_WithVariousValidCredentials_ReturnsJwtToken(string email, string password)
    {
        // Arrange
        var request = new PartnerLoginRequest
        {
            Email = email,
            Password = password
        };

        var partner = new Partner
        {
            Id = 1,
            Name = "Test Partner",
            Address = "Test Address",
            Email = email,
            Password = "$2a$12$hashedpassword",
            IsActive = true
        };

        _mockPartnerRepository
            .Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(partner);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, partner.Password))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt-token", result.Jwt);
    }

    #endregion
}
