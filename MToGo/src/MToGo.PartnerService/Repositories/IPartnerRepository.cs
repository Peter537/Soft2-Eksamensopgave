using MToGo.PartnerService.Entities;

namespace MToGo.PartnerService.Repositories;

public interface IPartnerRepository
{
    Task<Partner> CreateAsync(Partner partner);
    Task<bool> EmailExistsAsync(string email);
    Task<Partner?> GetByEmailAsync(string email);
    Task<Partner?> GetByIdAsync(int partnerId);
    Task<Partner?> GetByIdIncludeInactiveAsync(int partnerId);
    Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId);
    Task<MenuItem> AddMenuItemAsync(MenuItem menuItem);
    Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem);
    Task DeleteMenuItemAsync(MenuItem menuItem);
    Task<IEnumerable<Partner>> GetAllActivePartnersAsync();
    Task<Partner?> GetPartnerWithMenuAsync(int partnerId);
    Task UpdatePartnerActiveStatusAsync(int partnerId, bool isActive);
}
