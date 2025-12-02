using MToGo.ManagementService.Models;

namespace MToGo.ManagementService.Services;

public interface IManagementService
{
    Task<ManagementLoginResponse> LoginAsync(ManagementLoginRequest request);
}
