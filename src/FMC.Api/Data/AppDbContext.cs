using Fmc.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConsumerUser> ConsumerUsers => Set<ConsumerUser>();
    public DbSet<EnterpriseUser> EnterpriseUsers => Set<EnterpriseUser>();
    public DbSet<Cafeteria> Cafeterias => Set<Cafeteria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsumerUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<Cafeteria>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<EnterpriseUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasOne(x => x.Cafeteria)
                .WithOne(c => c.EnterpriseUser)
                .HasForeignKey<EnterpriseUser>(x => x.CafeteriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
