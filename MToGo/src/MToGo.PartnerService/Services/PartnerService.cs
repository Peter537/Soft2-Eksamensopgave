using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Repositories;
using MToGo.Shared.Security.Password;

namespace MToGo.PartnerService.Services;

public class PartnerService : IPartnerService
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IPasswordHasher _passwordHasher;

    public PartnerService(
        IPartnerRepository partnerRepository,
        IPasswordHasher passwordHasher)
    {
        _partnerRepository = partnerRepository;
        _passwordHasher = passwordHasher;
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
}
