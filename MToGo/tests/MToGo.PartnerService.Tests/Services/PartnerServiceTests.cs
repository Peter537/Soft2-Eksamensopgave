using Moq;
using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Repositories;
using MToGo.PartnerService.Services;
using MToGo.Shared.Security.Password;

namespace MToGo.PartnerService.Tests.Services;

public class PartnerServiceTests
{
    private readonly Mock<IPartnerRepository> _mockPartnerRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly PartnerService.Services.PartnerService _sut;

    public PartnerServiceTests()
    {
        _mockPartnerRepository = new Mock<IPartnerRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _sut = new PartnerService.Services.PartnerService(
            _mockPartnerRepository.Object,
            _mockPasswordHasher.Object
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
}
