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
    public DbSet<Follower> Followers => Set<Follower>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<UserEagleFeather> UserEagleFeathers => Set<UserEagleFeather>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeCompletion> ChallengeCompletions => Set<ChallengeCompletion>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Nickname).IsUnique();

            entity.HasOne(u => u.Organisation)
                .WithMany(o => o.Members)
                .HasForeignKey(u => u.OrganisationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EagleFeather>(entity =>
        {
            entity.HasIndex(e => new { e.Light, e.Section, e.Number }).IsUnique();
        });

        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.HasIndex(o => o.InviteCode).IsUnique();

            entity.HasOne(o => o.Leader)
                .WithMany()
                .HasForeignKey(o => o.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Follower>(entity =>
        {
            entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

            entity.HasOne(f => f.FollowerUser)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.FollowingUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserEagleFeather>(entity =>
        {
            entity.HasIndex(uef => new { uef.UserId, uef.EagleFeatherId }).IsUnique();

            entity.HasOne(uef => uef.User)
                .WithMany(u => u.EagleFeathers)
                .HasForeignKey(uef => uef.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uef => uef.EagleFeather)
                .WithMany(ef => ef.UserEagleFeathers)
                .HasForeignKey(uef => uef.EagleFeatherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uef => uef.VerifiedBy)
                .WithMany()
                .HasForeignKey(uef => uef.VerifiedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasOne(c => c.EagleFeather)
                .WithMany(ef => ef.Challenges)
                .HasForeignKey(c => c.EagleFeatherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChallengeCompletion>(entity =>
        {
            entity.HasIndex(cc => new { cc.UserId, cc.ChallengeId }).IsUnique();

            entity.HasOne(cc => cc.User)
                .WithMany(u => u.ChallengeCompletions)
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cc => cc.Challenge)
                .WithMany(c => c.Completions)
                .HasForeignKey(cc => cc.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Organisation)
                .WithMany(o => o.Posts)
                .HasForeignKey(p => p.OrganisationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.EagleFeather)
                .WithMany()
                .HasForeignKey(p => p.EagleFeatherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.ChallengeCompletion)
                .WithMany(cc => cc.Posts)
                .HasForeignKey(p => p.ChallengeCompletionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasIndex(l => new { l.UserId, l.LikeableType, l.LikeableId }).IsUnique();
            entity.HasIndex(l => new { l.LikeableType, l.LikeableId });

            entity.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(l => l.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(c => new { c.CommentableType, c.CommentableId });

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
