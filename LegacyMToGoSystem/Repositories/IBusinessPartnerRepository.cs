using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public interface IBusinessPartnerRepository
{
    Task<BusinessPartner?> GetByIdAsync(int id);
    Task<IEnumerable<BusinessPartner>> GetAllAsync();
    Task<BusinessPartner> CreateAsync(BusinessPartner partner);
}
