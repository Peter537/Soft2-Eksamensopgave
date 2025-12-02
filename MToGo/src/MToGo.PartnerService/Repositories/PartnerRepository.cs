using Microsoft.EntityFrameworkCore;
using MToGo.PartnerService.Data;
using MToGo.PartnerService.Entities;

namespace MToGo.PartnerService.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly PartnerDbContext _context;

    public PartnerRepository(PartnerDbContext context)
    {
        _context = context;
    }

    public async Task<Partner> CreateAsync(Partner partner)
    {
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();
        return partner;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Partners.AnyAsync(p => p.Email == email && !p.IsDeleted);
    }

    public async Task<Partner?> GetByEmailAsync(string email)
    {
        return await _context.Partners
            .FirstOrDefaultAsync(p => p.Email == email && !p.IsDeleted && p.IsActive);
    }

    public async Task<Partner?> GetByIdAsync(int partnerId)
    {
        return await _context.Partners
            .Include(p => p.MenuItems.Where(m => m.IsActive))
            .FirstOrDefaultAsync(p => p.Id == partnerId && !p.IsDeleted && p.IsActive);
    }

    public async Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId)
    {
        return await _context.MenuItems
            .FirstOrDefaultAsync(m => m.Id == menuItemId && m.IsActive);
    }

    public async Task<MenuItem> AddMenuItemAsync(MenuItem menuItem)
    {
        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();
        return menuItem;
    }

    public async Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem)
    {
        menuItem.UpdatedAt = DateTime.UtcNow;
        _context.MenuItems.Update(menuItem);
        await _context.SaveChangesAsync();
        return menuItem;
    }

    public async Task DeleteMenuItemAsync(MenuItem menuItem)
    {
        // Soft delete - set IsActive to false
        menuItem.IsActive = false;
        menuItem.UpdatedAt = DateTime.UtcNow;
        _context.MenuItems.Update(menuItem);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Partner>> GetAllActivePartnersAsync()
    {
        return await _context.Partners
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Partner?> GetPartnerWithMenuAsync(int partnerId)
    {
        return await _context.Partners
            .Include(p => p.MenuItems.Where(m => m.IsActive))
            .FirstOrDefaultAsync(p => p.Id == partnerId && !p.IsDeleted);
    }
}
