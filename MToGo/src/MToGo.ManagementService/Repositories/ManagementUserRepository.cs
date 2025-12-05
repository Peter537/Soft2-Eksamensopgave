using Microsoft.EntityFrameworkCore;
using MToGo.ManagementService.Data;
using MToGo.ManagementService.Entities;

namespace MToGo.ManagementService.Repositories;

public interface IManagementUserRepository
{
    Task<ManagementUser?> GetByUsernameAsync(string username);
    Task<ManagementUser?> GetByIdAsync(int id);
    Task<ManagementUser> CreateAsync(ManagementUser user);
    Task<bool> UsernameExistsAsync(string username);
}

public class ManagementUserRepository : IManagementUserRepository
{
    private readonly ManagementDbContext _context;

    public ManagementUserRepository(ManagementDbContext context)
    {
        _context = context;
    }

    public async Task<ManagementUser?> GetByUsernameAsync(string username)
    {
        return await _context.ManagementUsers
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<ManagementUser?> GetByIdAsync(int id)
    {
        return await _context.ManagementUsers
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<ManagementUser> CreateAsync(ManagementUser user)
    {
        _context.ManagementUsers.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.ManagementUsers.AnyAsync(u => u.Username == username && u.IsActive);
    }
}
