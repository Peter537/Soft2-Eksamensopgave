using Microsoft.EntityFrameworkCore;

namespace MToGo.LogCollectorService.Entities
{
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
        {
        }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ServiceName);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.Resource);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => new { e.Timestamp, e.ServiceName });

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LogId).HasColumnName("log_id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.ServiceName).HasColumnName("service_name");
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UserRole).HasColumnName("user_role");
                entity.Property(e => e.Action).HasColumnName("action");
                entity.Property(e => e.Resource).HasColumnName("resource");
                entity.Property(e => e.ResourceId).HasColumnName("resource_id");
                entity.Property(e => e.TraceId).HasColumnName("trace_id");
                entity.Property(e => e.PropertiesJson).HasColumnName("properties_json");
                entity.Property(e => e.MachineName).HasColumnName("machine_name");
            });
        }
    }
}
