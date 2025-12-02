using MToGo.PartnerService.Entities;

namespace MToGo.PartnerService.Repositories;

public interface IPartnerRepository
{
    Task<Partner> CreateAsync(Partner partner);
    Task<bool> EmailExistsAsync(string email);
    Task<Partner?> GetByEmailAsync(string email);
}
