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
}
