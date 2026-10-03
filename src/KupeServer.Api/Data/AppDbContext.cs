using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Countries.Entities;
using KupeServer.Api.Features.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace KupeServer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ExternalIdentity> ExternalIdentities { get; set; } = null!;
    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<Place> Places { get; set; } = null!;
    public DbSet<PlaceName> PlaceNames { get; set; } = null!;
    public DbSet<Language> Languages { get; set; } = null!;
    public DbSet<CountryLanguage> CountryLanguages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}