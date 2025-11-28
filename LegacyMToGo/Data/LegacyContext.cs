using LegacyMToGo.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyMToGo.Data;

public class LegacyContext(DbContextOptions<LegacyContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();
    }
}
