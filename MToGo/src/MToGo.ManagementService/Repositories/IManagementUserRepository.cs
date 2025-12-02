using MToGo.ManagementService.Entities;

namespace MToGo.ManagementService.Repositories;

public interface IManagementUserRepository
{
    Task<ManagementUser?> GetByUsernameAsync(string username);
    Task<ManagementUser?> GetByIdAsync(int id);
    Task<ManagementUser> CreateAsync(ManagementUser user);
    Task<bool> UsernameExistsAsync(string username);
}
