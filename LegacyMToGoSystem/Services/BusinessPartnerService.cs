using LegacyMToGoSystem.DTOs;
using LegacyMToGoSystem.Models;
using LegacyMToGoSystem.Repositories;

namespace LegacyMToGoSystem.Services;

public interface IBusinessPartnerService
{
    Task<IEnumerable<BusinessPartnerResponseDto>> GetAllPartnersAsync();
    Task<MenuResponseDto?> GetPartnerMenuAsync(int partnerId);
    Task<BusinessPartnerResponseDto> CreatePartnerAsync(CreateBusinessPartnerDto dto);
}

public class BusinessPartnerService : IBusinessPartnerService
{
    private readonly IBusinessPartnerRepository _repository;

    public BusinessPartnerService(IBusinessPartnerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BusinessPartnerResponseDto>> GetAllPartnersAsync()
    {
        var partners = await _repository.GetAllAsync();
        return partners.Select(p => new BusinessPartnerResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Address = p.Address,
            CuisineType = p.CuisineType,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<MenuResponseDto?> GetPartnerMenuAsync(int partnerId)
    {
        var partner = await _repository.GetByIdAsync(partnerId);
        if (partner == null) return null;

        return new MenuResponseDto
        {
            BusinessPartnerId = partner.Id,
            BusinessPartnerName = partner.Name,
            MenuItems = partner.MenuItems.Select(m => new MenuItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                Category = m.Category,
                IsAvailable = m.IsAvailable
            }).ToList()
        };
    }

    public async Task<BusinessPartnerResponseDto> CreatePartnerAsync(CreateBusinessPartnerDto dto)
    {
        var partner = new BusinessPartner
        {
            Name = dto.Name,
            Address = dto.Address,
            CuisineType = dto.CuisineType,
            MenuItems = dto.MenuItems?.Select(m => new MenuItem
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                Category = m.Category,
                IsAvailable = m.IsAvailable
            }).ToList() ?? new List<MenuItem>()
        };

        var created = await _repository.CreateAsync(partner);

        return new BusinessPartnerResponseDto
        {
            Id = created.Id,
            Name = created.Name,
            Address = created.Address,
            CuisineType = created.CuisineType,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt
        };
    }
}
