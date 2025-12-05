using Microsoft.EntityFrameworkCore;

namespace MToGo.FeedbackHubService.Entities;

public class FeedbackHubDbContext : DbContext
{
    public FeedbackHubDbContext(DbContextOptions<FeedbackHubDbContext> options) : base(options)
    {
    }

    public DbSet<Review> Reviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OrderId)
                .HasColumnName("order_id")
                .IsRequired();

            entity.Property(e => e.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired();

            entity.Property(e => e.PartnerId)
                .HasColumnName("partner_id")
                .IsRequired();

            entity.Property(e => e.AgentId)
                .HasColumnName("agent_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.FoodRating)
                .HasColumnName("food_rating")
                .IsRequired();

            entity.Property(e => e.AgentRating)
                .HasColumnName("agent_rating")
                .IsRequired();

            entity.Property(e => e.OrderRating)
                .HasColumnName("order_rating")
                .IsRequired();

            entity.Property(e => e.FoodComment)
                .HasColumnName("food_comment")
                .HasMaxLength(500);

            entity.Property(e => e.AgentComment)
                .HasColumnName("agent_comment")
                .HasMaxLength(500);

            entity.Property(e => e.OrderComment)
                .HasColumnName("order_comment")
                .HasMaxLength(500);

            // Index for querying reviews by order
            entity.HasIndex(e => e.OrderId)
                .IsUnique();

            // Index for querying reviews by partner
            entity.HasIndex(e => e.PartnerId);

            // Index for querying reviews by agent
            entity.HasIndex(e => e.AgentId);
        });
    }
}
