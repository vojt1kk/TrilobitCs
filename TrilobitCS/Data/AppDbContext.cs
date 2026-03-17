using Microsoft.EntityFrameworkCore;
using TrilobitCS.Models;

namespace TrilobitCS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EagleFeather> EagleFeathers => Set<EagleFeather>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EagleFeather>(entity =>
        {
            // Unique index na Light + Section + Number (= composite unique key pro updateOrCreate)
            entity.HasIndex(e => new { e.Light, e.Section, e.Number }).IsUnique();
        });
    }
}
