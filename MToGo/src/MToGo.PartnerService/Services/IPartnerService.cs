using MToGo.PartnerService.Models;

namespace MToGo.PartnerService.Services;

public interface IPartnerService
{
    Task<CreatePartnerResponse> RegisterPartnerAsync(PartnerRegisterRequest request);
    Task<PartnerLoginResponse> LoginAsync(PartnerLoginRequest request);
    Task<PartnerDetailsResponse?> GetPartnerByIdAsync(int partnerId);
    Task<CreateMenuItemResponse> AddMenuItemAsync(int partnerId, CreateMenuItemRequest request);
    Task UpdateMenuItemAsync(int partnerId, int menuItemId, UpdateMenuItemRequest request);
    Task DeleteMenuItemAsync(int partnerId, int menuItemId);
    Task<IEnumerable<PublicPartnerResponse>> GetAllActivePartnersAsync();
    Task<PublicMenuResponse?> GetPartnerMenuAsync(int partnerId);
    Task<PublicMenuItemResponse?> GetMenuItemAsync(int partnerId, int menuItemId);
}
