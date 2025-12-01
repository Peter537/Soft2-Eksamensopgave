using MToGo.PartnerService.Models;

namespace MToGo.PartnerService.Services;

public interface IPartnerService
{
    Task<CreatePartnerResponse> RegisterPartnerAsync(PartnerRegisterRequest request);
}
