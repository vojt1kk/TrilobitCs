using Microsoft.EntityFrameworkCore;
using TrilobitCS.Auth;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Services;

// Deterministic demo dataset: every row is looked up by a fixed natural key (nickname, org
// name, or a UserEagleFeatherId/LikeableId/CommentableId combination) before writing, so
// repeated runs never duplicate rows. Mutable fields (org membership, UEF moderation status)
// are re-synced to the spec on every run so the demo self-heals after being poked at through
// the API; CreatedAt/EarnedAt are only ever set once, at first insert, so timestamps don't drift.
public class DemoDataSeeder(AppDbContext db, BcryptPasswordHasher passwordHasher, ILogger<DemoDataSeeder> logger)
{
    private const string SeedPassword = "SeedDemo123!";
    private const string ContentTag = "[seed]";

    private sealed record UserSpec(
        string Nickname, string FirstName, string LastName, string Email,
        Gender Gender, DateOnly BirthDate, int OrgIndex, bool IsLeader);

    private sealed record OrgSpec(string Name, string Description);

    private sealed record FeatherAssignmentSpec(
        string UserNickname, int FeatherIndex, bool IsGrandChallenge,
        EagleFeatherStatus Status, string? VerifiedByNickname, string? ModeratorNote);

    private static readonly UserSpec[] UserSpecs =
    [
        new("seed01", "Adam", "Vlk", "seed01@seed.trilobit.dev", Gender.Male, new DateOnly(1995, 3, 12), 0, true),
        new("seed02", "Bára", "Sokolová", "seed02@seed.trilobit.dev", Gender.Female, new DateOnly(1993, 7, 4), 1, true),
        new("seed03", "Cyril", "Novák", "seed03@seed.trilobit.dev", Gender.Male, new DateOnly(2012, 1, 20), 0, false),
        new("seed04", "Dana", "Kovářová", "seed04@seed.trilobit.dev", Gender.Female, new DateOnly(2011, 5, 9), 0, false),
        new("seed05", "Erik", "Malý", "seed05@seed.trilobit.dev", Gender.Male, new DateOnly(2013, 11, 2), 0, false),
        new("seed06", "Filip", "Horák", "seed06@seed.trilobit.dev", Gender.Male, new DateOnly(2010, 9, 17), 1, false),
        new("seed07", "Gita", "Veselá", "seed07@seed.trilobit.dev", Gender.Female, new DateOnly(2012, 6, 30), 1, false),
        new("seed08", "Hugo", "Beneš", "seed08@seed.trilobit.dev", Gender.Male, new DateOnly(2011, 2, 14), 1, false),
    ];

    private static readonly OrgSpec[] OrgSpecs =
    [
        new("Seed oddíl Vydra", "[seed] Demo organizace s ukázkovými daty."),
        new("Seed oddíl Sokol", "[seed] Demo organizace s ukázkovými daty."),
    ];

    private static readonly (string Follower, string Following)[] FollowerPairs =
    [
        ("seed01", "seed03"), ("seed01", "seed04"), ("seed01", "seed05"),
        ("seed01", "seed06"), ("seed01", "seed07"),
        ("seed03", "seed01"), ("seed06", "seed02"),
    ];

    private static readonly FeatherAssignmentSpec[] FeatherAssignments =
    [
        new("seed01", 0, false, EagleFeatherStatus.Approved, "seed02", null),
        new("seed03", 1, false, EagleFeatherStatus.Approved, "seed01", null),
        new("seed04", 2, false, EagleFeatherStatus.Approved, "seed01", null),
        new("seed06", 3, false, EagleFeatherStatus.Approved, "seed02", null),
        new("seed07", 4, false, EagleFeatherStatus.Pending, null, null),
        new("seed08", 0, true, EagleFeatherStatus.Rejected, "seed02", "[seed] Potřeba doplnit fotky."),
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        var users = await SeedUsersAsync(ct);
        var orgs = await SeedOrganisationsAsync(users, ct);
        await SyncUserOrganisationsAsync(users, orgs, ct);
        await SeedFollowersAsync(users, ct);
        var approvedUefs = await SeedUserEagleFeathersAsync(users, ct);
        await SeedPostsWithEngagementAsync(users, approvedUefs, ct);

        logger.LogInformation("Demo data seeding pass complete.");
    }

    private async Task<Dictionary<string, User>> SeedUsersAsync(CancellationToken ct)
    {
        var nicknames = UserSpecs.Select(s => s.Nickname).ToArray();
        var users = await db.Users
            .Where(u => nicknames.Contains(u.Nickname))
            .ToDictionaryAsync(u => u.Nickname, ct);

        foreach (var spec in UserSpecs)
        {
            if (users.TryGetValue(spec.Nickname, out var user))
            {
                user.FirstName = spec.FirstName;
                user.LastName = spec.LastName;
                user.Gender = spec.Gender;
                user.BirthDate = spec.BirthDate;
                continue;
            }

            user = new User
            {
                Nickname = spec.Nickname,
                FirstName = spec.FirstName,
                LastName = spec.LastName,
                Email = spec.Email,
                Password = passwordHasher.Hash(SeedPassword),
                Gender = spec.Gender,
                BirthDate = spec.BirthDate,
                Role = spec.IsLeader ? UserRole.Leader : UserRole.User,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            users[spec.Nickname] = user;
        }

        await db.SaveChangesAsync(ct);
        return users;
    }

    private async Task<Organisation[]> SeedOrganisationsAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        var names = OrgSpecs.Select(s => s.Name).ToArray();
        var byName = await db.Organisations
            .Where(o => names.Contains(o.Name))
            .ToDictionaryAsync(o => o.Name, ct);

        var orgs = new Organisation[OrgSpecs.Length];
        for (var i = 0; i < OrgSpecs.Length; i++)
        {
            var spec = OrgSpecs[i];
            if (byName.TryGetValue(spec.Name, out var org))
            {
                org.Description = spec.Description;
                orgs[i] = org;
                continue;
            }

            var leaderNickname = UserSpecs.First(u => u.IsLeader && u.OrgIndex == i).Nickname;
            org = new Organisation
            {
                Name = spec.Name,
                Description = spec.Description,
                LeaderId = users[leaderNickname].Id,
                CreatedAt = DateTime.UtcNow,
            };
            db.Organisations.Add(org);
            orgs[i] = org;
        }

        await db.SaveChangesAsync(ct);
        return orgs;
    }

    // Self-heals seed users back into their spec'd org even if a demo session left/removed them.
    private async Task SyncUserOrganisationsAsync(Dictionary<string, User> users, Organisation[] orgs, CancellationToken ct)
    {
        foreach (var spec in UserSpecs)
            users[spec.Nickname].OrganisationId = orgs[spec.OrgIndex].Id;

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedFollowersAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        var userIds = FollowerPairs.SelectMany(p => new[] { users[p.Follower].Id, users[p.Following].Id }).Distinct().ToArray();
        var existing = await db.Followers
            .Where(f => userIds.Contains(f.FollowerId) && userIds.Contains(f.FollowingId))
            .Select(f => new { f.FollowerId, f.FollowingId })
            .ToListAsync(ct);
        var existingSet = existing.Select(f => (f.FollowerId, f.FollowingId)).ToHashSet();

        foreach (var (followerNick, followingNick) in FollowerPairs)
        {
            var followerId = users[followerNick].Id;
            var followingId = users[followingNick].Id;
            if (existingSet.Contains((followerId, followingId))) continue;

            db.Followers.Add(new Follower
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<UserEagleFeather>> SeedUserEagleFeathersAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        var maxIndex = FeatherAssignments.Max(a => a.FeatherIndex);
        var feathers = await db.EagleFeathers.OrderBy(f => f.Id).Take(maxIndex + 1).ToListAsync(ct);

        if (feathers.Count == 0)
        {
            logger.LogWarning("No eagle feathers found — skipping UserEagleFeather/Post/Comment/Like demo data. Run 'dotnet run -- scrape' first.");
            return [];
        }

        var userIds = UserSpecs.Select(s => users[s.Nickname].Id).ToArray();
        var featherIds = feathers.Select(f => f.Id).ToArray();
        var existing = await db.UserEagleFeathers
            .Where(uef => userIds.Contains(uef.UserId) && featherIds.Contains(uef.EagleFeatherId))
            .ToDictionaryAsync(uef => (uef.UserId, uef.EagleFeatherId), ct);

        var approved = new List<UserEagleFeather>();

        foreach (var spec in FeatherAssignments)
        {
            if (spec.FeatherIndex >= feathers.Count) continue;

            var user = users[spec.UserNickname];
            var feather = feathers[spec.FeatherIndex];
            var verifiedById = spec.VerifiedByNickname is null ? (int?)null : users[spec.VerifiedByNickname].Id;

            if (!existing.TryGetValue((user.Id, feather.Id), out var uef))
            {
                uef = new UserEagleFeather
                {
                    UserId = user.Id,
                    EagleFeatherId = feather.Id,
                    IsGrandChallenge = spec.IsGrandChallenge,
                    CreatedAt = DateTime.UtcNow,
                };
                db.UserEagleFeathers.Add(uef);
                existing[(user.Id, feather.Id)] = uef;
            }

            uef.Status = spec.Status;
            uef.VerifiedById = verifiedById;
            uef.ModeratorNote = spec.ModeratorNote;
            if (spec.Status == EagleFeatherStatus.Approved)
                uef.EarnedAt ??= DateTime.UtcNow;
            else
                uef.EarnedAt = null;

            if (spec.Status == EagleFeatherStatus.Approved)
                approved.Add(uef);
        }

        await db.SaveChangesAsync(ct);
        return approved;
    }

    private async Task SeedPostsWithEngagementAsync(Dictionary<string, User> users, List<UserEagleFeather> approvedUefs, CancellationToken ct)
    {
        if (approvedUefs.Count == 0) return;

        var uefIds = approvedUefs.Select(u => u.Id).ToArray();
        var existingPosts = await db.Posts
            .Where(p => uefIds.Contains(p.UserEagleFeatherId))
            .ToDictionaryAsync(p => p.UserEagleFeatherId, ct);

        var feathersById = await db.EagleFeathers
            .Where(f => approvedUefs.Select(u => u.EagleFeatherId).Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        foreach (var uef in approvedUefs)
        {
            uef.IsCompleted = true;

            if (existingPosts.TryGetValue(uef.Id, out var post)) continue;

            var author = users.Values.First(u => u.Id == uef.UserId);
            var feather = feathersById[uef.EagleFeatherId];

            post = new Post
            {
                UserId = author.Id,
                UserEagleFeatherId = uef.Id,
                Content = $"{ContentTag} {author.FirstName} splnil/a čin \"{feather.Name}\".",
                CreatedAt = DateTime.UtcNow,
            };
            db.Posts.Add(post);
            existingPosts[uef.Id] = post;
        }

        await db.SaveChangesAsync(ct);

        foreach (var post in existingPosts.Values)
            await SeedPostEngagementAsync(post, users, ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedPostEngagementAsync(Post post, Dictionary<string, User> users, CancellationToken ct)
    {
        var author = users.Values.First(u => u.Id == post.UserId);
        var commenterNickname = UserSpecs.First(s => s.Nickname != author.Nickname).Nickname;
        var commenter = users[commenterNickname];

        var commentContent = $"{ContentTag} Skvělá práce, {author.FirstName}!";
        var hasComment = await db.Comments.AnyAsync(c =>
            c.CommentableType == CommentableType.Posts &&
            c.CommentableId == post.Id &&
            c.UserId == commenter.Id &&
            c.Content == commentContent, ct);

        if (!hasComment)
        {
            db.Comments.Add(new Comment
            {
                UserId = commenter.Id,
                CommentableType = CommentableType.Posts,
                CommentableId = post.Id,
                Content = commentContent,
                CreatedAt = DateTime.UtcNow,
            });
        }

        var likerNicknames = UserSpecs
            .Select(s => s.Nickname)
            .Where(n => n != author.Nickname)
            .Take(2);

        foreach (var likerNickname in likerNicknames)
        {
            var liker = users[likerNickname];
            var hasLike = await db.Likes.AnyAsync(l =>
                l.LikeableType == LikeableType.Posts &&
                l.LikeableId == post.Id &&
                l.UserId == liker.Id, ct);

            if (hasLike) continue;

            db.Likes.Add(new Like
            {
                UserId = liker.Id,
                LikeableType = LikeableType.Posts,
                LikeableId = post.Id,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }
}
