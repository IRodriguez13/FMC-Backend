using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConsumerUser> ConsumerUsers => Set<ConsumerUser>();
    public DbSet<EnterpriseUser> EnterpriseUsers => Set<EnterpriseUser>();
    public DbSet<Cafeteria> Cafeterias => Set<Cafeteria>();
    public DbSet<CafeteriaPhoto> CafeteriaPhotos => Set<CafeteriaPhoto>();
    public DbSet<CafeteriaReview> CafeteriaReviews => Set<CafeteriaReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsumerUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.Property(x => x.AvatarStorageKey).HasMaxLength(260);
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

        modelBuilder.Entity<CafeteriaPhoto>(e =>
        {
            e.Property(x => x.StorageKey).HasMaxLength(260);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.Property(x => x.AuthorRole).HasMaxLength(32);
            e.HasIndex(x => x.CafeteriaId);
            e.HasOne(x => x.Cafeteria)
                .WithMany()
                .HasForeignKey(x => x.CafeteriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CafeteriaReview>(e =>
        {
            e.Property(x => x.AuthorRole).HasMaxLength(32);
            e.Property(x => x.Text).HasMaxLength(2000);
            e.HasIndex(x => x.CafeteriaId);
            e.HasIndex(x => new { x.CafeteriaId, x.AuthorUserId, x.AuthorRole }).IsUnique();
            e.HasOne(x => x.Cafeteria)
                .WithMany()
                .HasForeignKey(x => x.CafeteriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
