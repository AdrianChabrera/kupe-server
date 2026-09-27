using KupeServer.Api.Features.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace KupeServer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
}