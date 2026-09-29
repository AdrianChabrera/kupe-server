using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace KupeServer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ExternalIdentity> ExternalIdentities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderUserId).HasMaxLength(255).IsRequired();

            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();

            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalIdentities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}