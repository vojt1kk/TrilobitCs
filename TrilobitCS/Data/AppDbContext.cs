using Microsoft.EntityFrameworkCore;
using TrilobitCS.Models;

namespace TrilobitCS.Data;

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
    public DbSet<UserChallenge> UserChallenges => Set<UserChallenge>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<OrganisationInvite> OrganisationInvites => Set<OrganisationInvite>();

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

            entity.Property(uef => uef.IsCompleted)
                .HasDefaultValue(false);

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
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserChallenge>(entity =>
        {
            entity.HasIndex(uc => new { uc.UserId, uc.ChallengeId }).IsUnique();

            entity.Property(uc => uc.PinnedAt)
                .HasDefaultValueSql("now()");

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserChallenges)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Challenge)
                .WithMany(c => c.UserChallenges)
                .HasForeignKey(uc => uc.ChallengeId)
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

            entity.HasOne(p => p.UserEagleFeather)
                .WithMany(uef => uef.Posts)
                .HasForeignKey(p => p.UserEagleFeatherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Challenge)
                .WithMany()
                .HasForeignKey(p => p.ChallengeId)
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
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(c => new { c.CommentableType, c.CommentableId });

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganisationInvite>(entity =>
        {
            // Partial unique using PostgreSQL NULL != NULL semantics on DeclinedAt:
            // a new pending invite (DeclinedAt = NULL) can coexist with a declined one (DeclinedAt != NULL)
            // for the same (InvitedUserId, OrganisationId) pair.
            entity.HasIndex(i => new { i.InvitedUserId, i.OrganisationId, i.DeclinedAt })
                .IsUnique();

            entity.HasOne(i => i.Organisation)
                .WithMany(o => o.Invites)
                .HasForeignKey(i => i.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.InvitedUser)
                .WithMany(u => u.ReceivedInvites)
                .HasForeignKey(i => i.InvitedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.InvitedBy)
                .WithMany()
                .HasForeignKey(i => i.InvitedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
