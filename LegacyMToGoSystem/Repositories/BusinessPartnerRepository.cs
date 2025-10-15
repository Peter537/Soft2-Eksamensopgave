using LegacyMToGoSystem.Infrastructure;
using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public class BusinessPartnerRepository : IBusinessPartnerRepository
{
    private readonly IDatabase _database;
    private const string CollectionName = "businesspartners";

    public BusinessPartnerRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<BusinessPartner?> GetByIdAsync(int id)
    {
        var partners = await LoadPartnersAsync();
        return partners.FirstOrDefault(p => p.Id == id && p.IsActive);
    }

    public async Task<IEnumerable<BusinessPartner>> GetAllAsync()
    {
        var partners = await LoadPartnersAsync();
        return partners.Where(p => p.IsActive).ToList();
    }

    public async Task<BusinessPartner> CreateAsync(BusinessPartner partner)
    {
        var partners = await LoadPartnersAsync();
        
        partner.Id = partners.Any() ? partners.Max(p => p.Id) + 1 : 1;
        partner.CreatedAt = DateTime.UtcNow;
        partner.IsActive = true;
        
        partners.Add(partner);
        await SavePartnersAsync(partners);
        
        return partner;
    }

    private async Task<List<BusinessPartner>> LoadPartnersAsync()
    {
        var partners = await _database.LoadAsync<List<BusinessPartner>>(CollectionName);
        return partners ?? new List<BusinessPartner>();
    }

    private async Task SavePartnersAsync(List<BusinessPartner> partners)
    {
        await _database.SaveAsync(CollectionName, partners);
    }
}
