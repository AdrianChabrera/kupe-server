using KupeServer.Api.Features.Countries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KupeServer.Api.Features.Countries.Data;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> e)
    {
        e.ToTable("country");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        e.Property(x => x.Iso2).HasMaxLength(2).IsFixedLength().IsRequired();
        e.HasIndex(x => x.Iso2).IsUnique();
        e.Property(x => x.Iso3).HasMaxLength(3).IsFixedLength().IsRequired();
        e.Property(x => x.Name).IsRequired();
        e.Property(x => x.Continent).HasMaxLength(2).IsFixedLength();
        e.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength();
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        e.HasIndex(x => x.GeonameId).IsUnique();
    }
}

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> e)
    {
        e.ToTable("place");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        e.HasIndex(x => x.GeonameId).IsUnique();
        e.Property(x => x.Name).IsRequired();
        e.Property(x => x.AsciiName).IsRequired();
        e.Property(x => x.Admin1Code).HasColumnName("admin1_code").HasMaxLength(20);
        e.Property(x => x.FeatureCode).HasMaxLength(10).IsRequired();
        e.Property(x => x.Population).HasDefaultValue(0L);
        e.Property(x => x.WikidataId).HasMaxLength(20);
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");

        e.HasIndex(x => new { x.CountryId, x.Population }).IsDescending(false, true);
        e.HasIndex(x => x.Admin1Code);
        e.HasIndex(x => new { x.Lat, x.Lon });
        e.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        e.HasIndex(x => x.AsciiName).HasMethod("gin").HasOperators("gin_trgm_ops");

        e.HasOne(x => x.Country)
            .WithMany(c => c.Places)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlaceNameConfiguration : IEntityTypeConfiguration<PlaceName>
{
    public void Configure(EntityTypeBuilder<PlaceName> e)
    {
        e.ToTable("place_name");
        e.HasKey(x => new { x.PlaceId, x.Language, x.Name });
        e.Property(x => x.Language).HasMaxLength(8);
        e.HasIndex(x => x.Language);
        e.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");

        e.HasOne(x => x.Place)
            .WithMany(p => p.Names)
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}