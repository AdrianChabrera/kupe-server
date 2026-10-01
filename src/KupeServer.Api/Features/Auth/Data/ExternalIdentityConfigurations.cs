using KupeServer.Api.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KupeServer.Api.Features.Auth.Data;

public class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> e)
    {
        e.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        e.Property(x => x.ProviderUserId).HasMaxLength(255).IsRequired();

        e.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        e.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();

        e.HasOne(x => x.User)
            .WithMany(u => u.ExternalIdentities)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}