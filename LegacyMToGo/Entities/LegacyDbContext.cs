using Microsoft.EntityFrameworkCore;

namespace LegacyMToGo.Entities;

public class LegacyDbContext(DbContextOptions<LegacyDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();
    }
}
