using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Logging;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Repositories;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.PartnerService.Services;

public class PartnerService : IPartnerService
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(
        IPartnerRepository partnerRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<PartnerService> logger)
    {
        _partnerRepository = partnerRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<CreatePartnerResponse> RegisterPartnerAsync(PartnerRegisterRequest request)
    {
        // Validate menu is not empty
        if (request.Menu == null || request.Menu.Count == 0)
        {
            throw new EmptyMenuException("Menu cannot be empty. At least one menu item is required.");
        }

        // Check for duplicate email
        if (await _partnerRepository.EmailExistsAsync(request.Email))
        {
            throw new DuplicateEmailException("A partner with this email already exists.");
        }

        var partner = new Partner
        {
            Name = request.Name,
            Address = request.Address,
            Email = request.Email,
            Password = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            MenuItems = request.Menu.Select(m => new MenuItem
            {
                Name = m.Name,
                Price = m.Price,
                IsActive = true
            }).ToList()
        };

        var createdPartner = await _partnerRepository.CreateAsync(partner);

        return new CreatePartnerResponse { Id = createdPartner.Id };
    }

    public async Task<PartnerLoginResponse> LoginAsync(PartnerLoginRequest request)
    {
        var partner = await _partnerRepository.GetByEmailAsync(request.Email);

        if (partner == null)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, partner.Password))
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var token = _jwtTokenService.GenerateToken(
            userId: partner.Id,
            email: partner.Email,
            role: UserRoles.Partner,
            name: partner.Name
        );

        return new PartnerLoginResponse { Jwt = token };
    }

    public async Task<PartnerDetailsResponse?> GetPartnerByIdAsync(int partnerId)
    {
        var partner = await _partnerRepository.GetByIdAsync(partnerId);
        if (partner == null)
        {
            return null;
        }

        return new PartnerDetailsResponse
        {
            Id = partner.Id,
            Name = partner.Name,
            Address = partner.Address,
            IsActive = partner.IsActive,
            MenuItems = partner.MenuItems.Select(m => new MenuItemResponse
            {
                Id = m.Id,
                Name = m.Name,
                Price = m.Price
            }).ToList()
        };
    }

    public async Task<CreateMenuItemResponse> AddMenuItemAsync(int partnerId, CreateMenuItemRequest request)
    {
        _logger.AddingMenuItem(partnerId);

        var partner = await _partnerRepository.GetByIdAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            throw new PartnerNotFoundException($"Partner with ID {partnerId} not found.");
        }

        var menuItem = new MenuItem
        {
            PartnerId = partnerId,
            Name = request.Name,
            Price = request.Price,
            IsActive = true
        };

        var createdMenuItem = await _partnerRepository.AddMenuItemAsync(menuItem);

        _logger.MenuItemAdded(partnerId, createdMenuItem.Id);

        return new CreateMenuItemResponse { Id = createdMenuItem.Id };
    }

    public async Task UpdateMenuItemAsync(int partnerId, int menuItemId, UpdateMenuItemRequest request)
    {
        _logger.UpdatingMenuItem(partnerId, menuItemId);

        var partner = await _partnerRepository.GetByIdAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            throw new PartnerNotFoundException($"Partner with ID {partnerId} not found.");
        }

        var menuItem = await _partnerRepository.GetMenuItemByIdAsync(menuItemId);
        if (menuItem == null)
        {
            _logger.MenuItemNotFound(menuItemId);
            throw new MenuItemNotFoundException($"Menu item with ID {menuItemId} not found.");
        }

        if (menuItem.PartnerId != partnerId)
        {
            _logger.MenuItemNotOwnedByPartner(menuItemId, partnerId);
            throw new UnauthorizedMenuItemAccessException($"Menu item {menuItemId} does not belong to partner {partnerId}.");
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            menuItem.Name = request.Name;
        }

        if (request.Price.HasValue)
        {
            menuItem.Price = request.Price.Value;
        }

        await _partnerRepository.UpdateMenuItemAsync(menuItem);

        _logger.MenuItemUpdated(partnerId, menuItemId);
    }

    public async Task DeleteMenuItemAsync(int partnerId, int menuItemId)
    {
        _logger.DeletingMenuItem(partnerId, menuItemId);

        var partner = await _partnerRepository.GetByIdAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            throw new PartnerNotFoundException($"Partner with ID {partnerId} not found.");
        }

        var menuItem = await _partnerRepository.GetMenuItemByIdAsync(menuItemId);
        if (menuItem == null)
        {
            _logger.MenuItemNotFound(menuItemId);
            throw new MenuItemNotFoundException($"Menu item with ID {menuItemId} not found.");
        }

        if (menuItem.PartnerId != partnerId)
        {
            _logger.MenuItemNotOwnedByPartner(menuItemId, partnerId);
            throw new UnauthorizedMenuItemAccessException($"Menu item {menuItemId} does not belong to partner {partnerId}.");
        }

        await _partnerRepository.DeleteMenuItemAsync(menuItem);

        _logger.MenuItemDeleted(partnerId, menuItemId);
    }

    public async Task<IEnumerable<PublicPartnerResponse>> GetAllActivePartnersAsync()
    {
        _logger.GettingAllActivePartners();

        var partners = await _partnerRepository.GetAllActivePartnersAsync();

        var result = partners.Select(p => new PublicPartnerResponse
        {
            Id = p.Id,
            Name = p.Name,
            Address = p.Address
        });

        _logger.ActivePartnersRetrieved(partners.Count());

        return result;
    }

    public async Task<PublicMenuResponse?> GetPartnerMenuAsync(int partnerId)
    {
        _logger.GettingPartnerMenu(partnerId);

        var partner = await _partnerRepository.GetPartnerWithMenuAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            return null;
        }

        _logger.PartnerMenuRetrieved(partnerId, partner.MenuItems.Count);

        return new PublicMenuResponse
        {
            PartnerId = partner.Id,
            PartnerName = partner.Name,
            IsActive = partner.IsActive,
            Items = partner.MenuItems.Select(m => new PublicMenuItemResponse
            {
                Id = m.Id,
                Name = m.Name,
                Price = m.Price
            }).ToList()
        };
    }

    public async Task<PublicMenuItemResponse?> GetMenuItemAsync(int partnerId, int menuItemId)
    {
        _logger.GettingMenuItem(partnerId, menuItemId);

        var partner = await _partnerRepository.GetPartnerWithMenuAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            return null;
        }

        var menuItem = partner.MenuItems.FirstOrDefault(m => m.Id == menuItemId);
        if (menuItem == null)
        {
            _logger.MenuItemNotFound(menuItemId);
            return null;
        }

        _logger.MenuItemRetrieved(partnerId, menuItemId);

        return new PublicMenuItemResponse
        {
            Id = menuItem.Id,
            Name = menuItem.Name,
            Price = menuItem.Price
        };
    }

    public async Task<PartnerDetailsResponse?> GetPartnerByIdIncludeInactiveAsync(int partnerId)
    {
        var partner = await _partnerRepository.GetByIdIncludeInactiveAsync(partnerId);
        if (partner == null)
        {
            return null;
        }

        return new PartnerDetailsResponse
        {
            Id = partner.Id,
            Name = partner.Name,
            Address = partner.Address,
            IsActive = partner.IsActive,
            MenuItems = partner.MenuItems.Select(m => new MenuItemResponse
            {
                Id = m.Id,
                Name = m.Name,
                Price = m.Price
            }).ToList()
        };
    }

    public async Task<bool> SetPartnerActiveStatusAsync(int partnerId, bool isActive)
    {
        _logger.SettingPartnerActiveStatus(partnerId, isActive);

        var partner = await _partnerRepository.GetByIdIncludeInactiveAsync(partnerId);
        if (partner == null)
        {
            _logger.PartnerNotFound(partnerId);
            return false;
        }

        await _partnerRepository.UpdatePartnerActiveStatusAsync(partnerId, isActive);

        _logger.PartnerActiveStatusUpdated(partnerId, isActive);

        return true;
    }
}
