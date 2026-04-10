using Microsoft.EntityFrameworkCore;
using TrilobitCS.Models;

namespace TrilobitCS.Data;

// Laravel: Database\Migrations
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<EagleFeather> EagleFeathers => Set<EagleFeather>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Nickname).IsUnique();
        });

        modelBuilder.Entity<EagleFeather>(entity =>
        {
            entity.HasIndex(e => new { e.Light, e.Section, e.Number }).IsUnique();
        });
    }
}
