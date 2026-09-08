using Microsoft.EntityFrameworkCore;
using TrilobitCS.Auth;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Services;

// Deterministic demo dataset: users, organisations, a dense follower graph, eagle-feather
// attempts in every moderation state, posts with comments/replies/likes, challenges and
// organisation invites.
//
// Every row is looked up by a fixed natural key (nickname, org name, (user, feather),
// (post, commenter), ...) before writing, so repeated runs never duplicate rows — this
// matters because DemoDataSeederService re-runs the whole pass every 24 hours and on every
// startup, and five of the tables involved (organisations, announcements, posts, comments,
// challenges) have no DB-level uniqueness to fall back on.
//
// All variation comes from Prng below, never from Random/GetHashCode: .NET randomizes string
// hash codes per process, so a hash-seeded "deterministic" layout would reshuffle on every
// restart and duplicate every non-unique row. Mutable fields (org membership, UEF moderation
// status) are re-synced to the spec on each pass so the demo self-heals after being poked at
// through the API; CreatedAt/EarnedAt are only ever set at first insert, so timestamps don't drift.
//
// Demo credentials (login is by nickname):
//   admin / admin12345678   — Leader of the first organisation, follows and is followed by everyone
//   seedNN / SeedDemo123!   — the 40 generated members
public class DemoDataSeeder(AppDbContext db, BcryptPasswordHasher passwordHasher, ILogger<DemoDataSeeder> logger)
{
    private const string SeedPassword = "SeedDemo123!";
    private const string AdminNickname = "admin";
    private const string AdminPassword = "admin12345678";
    private const string ContentTag = "[seed]";

    private const int MemberCount = 40;
    private const int OrgCount = 5;
    private const int OrglessCount = 6;

    // Attempts are drawn from a fixed-size prefix of the feather catalog ordered by Id, and each
    // (user, feather) pair is decided independently at a fixed probability. Both details exist so
    // that running the scraper — which appends feathers with higher Ids — can only ever add new
    // attempts, never re-roll the ones already in the database.
    private const int FeatherPoolSize = 60;
    private const int FeatherAttemptPercent = 12;
    private const int AdminFeatherAttemptPercent = 30;
    private const int ChallengePinPercent = 25;

    // Independent PRNG streams — one per entity kind, so adding rows to one section never
    // reshuffles another section's layout on the next pass.
    private static class Stream
    {
        public const int Followers = 1;
        public const int UserEagleFeathers = 2;
        public const int Posts = 3;
        public const int Comments = 4;
        public const int Replies = 5;
        public const int PostLikes = 6;
        public const int CommentLikes = 7;
        public const int UserChallenges = 8;
    }

    // xorshift32. Not System.Random: its algorithm is documented as implementation-defined,
    // and a framework upgrade changing it would silently re-roll the entire dataset.
    private sealed class Prng(uint seed)
    {
        private uint _state = seed == 0 ? 0x9E3779B9u : seed;

        private uint NextUInt()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextUInt() % (uint)maxExclusive);

        public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

        public bool Chance(int percent) => Next(100) < percent;

        public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];
    }

    // Streams are seeded from stable entity identity (a database Id, or a spec position that only
    // moves when the spec itself changes) — never from a loop index over a list whose length can
    // grow, which would shift every downstream draw and re-seed rows that already exist.
    private static Prng PrngFor(int stream, int index) =>
        new(unchecked((uint)stream * 2654435761u + (uint)index * 40503u + 1u));

    // Engagement is dated relative to its target, which for a post published an hour ago would
    // otherwise put its comments and likes in the future.
    private static DateTime NotInFuture(DateTime value)
    {
        var now = DateTime.UtcNow;
        return value > now ? now : value;
    }

    private static Prng PrngFor(int stream, int a, int b) =>
        new(unchecked((uint)stream * 2654435761u + (uint)a * 40503u + (uint)b * 2246822519u + 1u));

    // Draws `count` distinct indices from [0, poolSize) without replacement (partial Fisher-Yates).
    private static int[] PickDistinct(Prng rng, int poolSize, int count, int? exclude = null)
    {
        var pool = new List<int>(poolSize);
        for (var i = 0; i < poolSize; i++)
            if (i != exclude)
                pool.Add(i);

        count = Math.Min(count, pool.Count);
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            var j = rng.Next(pool.Count);
            result[i] = pool[j];
            pool.RemoveAt(j);
        }

        return result;
    }

    private sealed record UserSpec(
        string Nickname, string FirstName, string LastName, string Email,
        Gender Gender, DateOnly BirthDate, int? OrgIndex, UserRole Role);

    private sealed record OrgSpec(string Name, string Description, string LeaderNickname);

    private sealed record AnnouncementSpec(string Title, string Content);

    private sealed record ChallengeSpec(string Title, string Description, int DifficultyLevel, int ValidFromDaysAgo, int ValidForDays);

    private static readonly string[] FirstNames =
    [
        "Adam", "Bára", "Cyril", "Dana", "Erik", "Filip", "Gita", "Hugo", "Iva", "Jakub",
        "Klára", "Lukáš", "Marek", "Nela", "Ondřej", "Petra", "Radek", "Simona", "Tomáš", "Urban",
        "Vojta", "Zuzana", "Alena", "Bohdan", "Cecílie", "David", "Eliška", "František", "Gabriela", "Honza",
        "Ivana", "Jiří", "Kamila", "Libor", "Michal", "Natálie", "Oskar", "Pavla", "Robert", "Sára",
    ];

    private static readonly string[] LastNames =
    [
        "Vlk", "Sokolová", "Novák", "Kovářová", "Malý", "Horák", "Veselá", "Beneš", "Dvořáková", "Černý",
        "Marková", "Pospíšil", "Kučera", "Bláhová", "Doležal", "Šimková", "Nováček", "Fialová", "Kraus", "Sýkora",
        "Liška", "Havlová", "Bartošová", "Zeman", "Kolářová", "Urban", "Machová", "Hruška", "Konečná", "Král",
        "Vávrová", "Procházka", "Tichá", "Ryba", "Sedláček", "Jelínková", "Vaněk", "Adamová", "Kopecký", "Bezruč",
    ];

    private static readonly UserSpec[] UserSpecs = BuildUserSpecs();

    private static readonly OrgSpec[] OrgSpecs = BuildOrgSpecs();

    private static readonly AnnouncementSpec[] AnnouncementSpecs =
    [
        new($"{ContentTag} Schůzka oddílu", "Příští schůzka bude ve čtvrtek v klubovně, sraz v 16:00."),
        new($"{ContentTag} Brigáda na základně", "Prosíme o účast na jarní brigádě, přineste pracovní rukavice."),
        new($"{ContentTag} Letní tábor", "Přihlášky na letní tábor spouštíme od pondělí, kapacita je omezená."),
        new($"{ContentTag} Změna programu", "Sobotní výprava se přesouvá na neděli kvůli počasí."),
        new($"{ContentTag} Nová orlí pera", "Do katalogu přibyla nová pera 3. světla — mrkněte na ně."),
    ];

    private static readonly ChallengeSpec[] ChallengeSpecs =
    [
        new($"{ContentTag} Zimní výzva", "Splň libovolné pero 1. světla venku v mrazu.", 1, 20, 60),
        new($"{ContentTag} Běžecká série", "Tři běhy během jednoho týdne.", 2, 15, 45),
        new($"{ContentTag} Noc pod širákem", "Přespi venku bez stanu a dokumentuj to.", 3, 10, 40),
        new($"{ContentTag} Turistický přejezd", "Ujdi 20 km v jednom dni.", 2, 30, 90),
        new($"{ContentTag} Znalost přírody", "Pozoruj a popiš deset druhů rostlin.", 1, 5, 60),
        new($"{ContentTag} Vodácký víkend", "Sjeď řeku na kánoi nebo raftu.", 2, 60, 30),
        new($"{ContentTag} Řemeslná dovednost", "Vyrob si vlastní nástroj nebo ozdobu.", 1, 45, 120),
        new($"{ContentTag} Velký čin roku", "Vyber si velký čin a dotáhni ho do konce.", 3, 90, 365),
    ];

    private static readonly string[] PostTemplates =
    [
        "Dneska jsem to konečně dal — {0}. Bylo to náročnější, než jsem čekal!",
        "Hotovo: {0}. Díky všem, co mi drželi palce.",
        "Trénoval jsem na {0} skoro měsíc a stálo to za to.",
        "{0} splněno. Nejtěžší byl začátek, pak už to šlo samo.",
        "Tak tohle byl zážitek — {0}. Rozhodně to zkuste taky.",
        "Zapisuju si {0}. Fotky z akce přikládám.",
        "Vedoucí říkal, že {0} nedám. Dal jsem.",
        "Po třech pokusech jsem zvládl {0}. Vytrvalost se vyplácí.",
        "Celý víkend jsem věnoval činu {0} a nelituju ani minuty.",
        "{0} — hotovo! Kdo se přidá k dalšímu peru?",
        "Splnil jsem {0} společně s kamarády z oddílu.",
        "Nejlepší část dneška: {0}. Už plánuju další.",
    ];

    private static readonly string[] CommentTemplates =
    [
        "Skvělá práce, {0}!",
        "To je fakt hustý, gratuluju!",
        "Já jsem na tomhle peru zasekl/a, jak jsi to zvládl/a?",
        "Paráda, přesně tohle chci zkusit taky.",
        "Klobouk dolů, {0}.",
        "Tohle bylo těžké, dobrá práce!",
        "Fotky vypadají skvěle.",
        "Konečně někdo, kdo to dotáhl do konce :)",
        "Příště jdeme do toho spolu.",
        "Držím palce u dalšího pera!",
    ];

    private static readonly string[] ReplyTemplates =
    [
        "Díky moc!",
        "Jasně, klidně ti s tím poradím.",
        "Zkus to rozdělit na víc dní, mně to pomohlo.",
        "Přesně tak, chce to hlavně trpělivost.",
        "Díky, příště jdeme spolu!",
    ];

    private static UserSpec[] BuildUserSpecs()
    {
        var specs = new List<UserSpec>(MemberCount + 1)
        {
            // Admin is the demo login: Leader of the first organisation, so leader-only endpoints
            // (org update, announcements, invites, UEF approvals) are all reachable from one account.
            new(AdminNickname, "Admin", "Trilobit", "admin@seed.trilobit.dev",
                Gender.Other, new DateOnly(1990, 1, 1), 0, UserRole.Leader),
        };

        for (var i = 1; i <= MemberCount; i++)
        {
            var firstName = FirstNames[(i - 1) % FirstNames.Length];
            var lastName = LastNames[(i - 1) % LastNames.Length];

            int? orgIndex;
            var role = UserRole.User;

            if (i < OrgCount)
            {
                // seed01..seed04 lead organisations 1..4 (organisation 0 is led by admin).
                orgIndex = i;
                role = UserRole.Leader;
            }
            else if (i > MemberCount - OrglessCount)
            {
                // Tail of the roster stays without an organisation so pending invites have
                // valid targets — the invite API 422s on users who already belong somewhere.
                orgIndex = null;
            }
            else
            {
                orgIndex = i % OrgCount;
                // Two extra Leaders without their own organisation (a superadmin-assigned role in
                // production) so approvals in the demo don't all carry the same verified_by.
                if (i is 5 or 6) role = UserRole.Leader;
            }

            var gender = lastName.EndsWith('á') ? Gender.Female : Gender.Male;
            if (i % 17 == 0) gender = Gender.Other;

            var birthDate = role == UserRole.Leader
                ? new DateOnly(1985 + i % 12, 1 + i % 12, 1 + i % 27)
                : new DateOnly(2008 + i % 8, 1 + i * 5 % 12, 1 + i * 7 % 27);

            specs.Add(new UserSpec($"seed{i:D2}", firstName, lastName, $"seed{i:D2}@seed.trilobit.dev",
                gender, birthDate, orgIndex, role));
        }

        return [.. specs];
    }

    private static OrgSpec[] BuildOrgSpecs()
    {
        string[] names = ["Seed oddíl Vydra", "Seed oddíl Sokol", "Seed oddíl Rys", "Seed oddíl Bobr", "Seed oddíl Jestřáb"];

        return [.. Enumerable.Range(0, OrgCount).Select(i => new OrgSpec(
            names[i],
            $"{ContentTag} Demo organizace s ukázkovými daty.",
            i == 0 ? AdminNickname : $"seed{i:D2}"))];
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        var users = await SeedUsersAsync(ct);
        var orgs = await SeedOrganisationsAsync(users, ct);
        await SyncUserOrganisationsAsync(users, orgs, ct);
        await SeedAnnouncementsAsync(users, orgs, ct);
        await SeedOrganisationInvitesAsync(users, orgs, ct);
        await SeedFollowersAsync(users, ct);

        var feathers = await db.EagleFeathers.OrderBy(f => f.Id).ToListAsync(ct);
        if (feathers.Count == 0)
        {
            logger.LogWarning(
                "No eagle feathers found — skipping challenge/UserEagleFeather/post/comment/like demo data. Run 'dotnet run -- scrape' first.");
            return;
        }

        var challenges = await SeedChallengesAsync(feathers, ct);
        await SeedUserChallengesAsync(users, challenges, ct);
        var uefs = await SeedUserEagleFeathersAsync(users, feathers, ct);
        var posts = await SeedPostsAsync(users, uefs, feathers, challenges, ct);
        var comments = await SeedCommentsAsync(users, posts, ct);
        await SeedLikesAsync(users, posts, comments, ct);

        logger.LogInformation(
            "Demo data seeding pass complete: {Users} users, {Orgs} organisations, {Uefs} eagle-feather attempts, {Posts} posts, {Comments} comments.",
            UserSpecs.Length, orgs.Length, uefs.Count, posts.Count, comments.Count);
    }

    private async Task<Dictionary<string, User>> SeedUsersAsync(CancellationToken ct)
    {
        var nicknames = UserSpecs.Select(s => s.Nickname).ToArray();
        var users = await db.Users
            .Where(u => nicknames.Contains(u.Nickname))
            .ToDictionaryAsync(u => u.Nickname, ct);

        // bcrypt is deliberately slow — hash each distinct demo password once per pass, not once per user.
        var adminHash = passwordHasher.Hash(AdminPassword);
        var memberHash = new Lazy<string>(() => passwordHasher.Hash(SeedPassword));

        var createdAtBase = DateTime.UtcNow.AddDays(-400);

        for (var i = 0; i < UserSpecs.Length; i++)
        {
            var spec = UserSpecs[i];
            var isAdmin = spec.Nickname == AdminNickname;

            if (users.TryGetValue(spec.Nickname, out var user))
            {
                user.FirstName = spec.FirstName;
                user.LastName = spec.LastName;
                user.Gender = spec.Gender;
                user.BirthDate = spec.BirthDate;
                user.Role = spec.Role;
                // The documented admin credentials must keep working even if the row was created
                // by an earlier seeder version or had its password changed through the API.
                if (isAdmin) user.Password = adminHash;
                continue;
            }

            user = new User
            {
                Nickname = spec.Nickname,
                FirstName = spec.FirstName,
                LastName = spec.LastName,
                Email = spec.Email,
                Password = isAdmin ? adminHash : memberHash.Value,
                Gender = spec.Gender,
                BirthDate = spec.BirthDate,
                Role = spec.Role,
                CreatedAt = createdAtBase.AddDays(i * 3),
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
            var leaderId = users[spec.LeaderNickname].Id;

            if (byName.TryGetValue(spec.Name, out var org))
            {
                org.Description = spec.Description;
                org.LeaderId = leaderId;
                orgs[i] = org;
                continue;
            }

            org = new Organisation
            {
                Name = spec.Name,
                Description = spec.Description,
                LeaderId = leaderId,
                CreatedAt = DateTime.UtcNow.AddDays(-380 + i * 10),
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
            users[spec.Nickname].OrganisationId = spec.OrgIndex is int index ? orgs[index].Id : null;

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedAnnouncementsAsync(Dictionary<string, User> users, Organisation[] orgs, CancellationToken ct)
    {
        var orgIds = orgs.Select(o => o.Id).ToArray();
        var titles = AnnouncementSpecs.Select(s => s.Title).ToArray();
        var existing = await db.Announcements
            .Where(a => orgIds.Contains(a.OrganisationId) && titles.Contains(a.Title))
            .ToDictionaryAsync(a => (a.OrganisationId, a.Title), ct);

        for (var orgIndex = 0; orgIndex < orgs.Length; orgIndex++)
        {
            var org = orgs[orgIndex];
            var leader = users[OrgSpecs[orgIndex].LeaderNickname];

            for (var i = 0; i < AnnouncementSpecs.Length; i++)
            {
                var spec = AnnouncementSpecs[i];
                if (existing.TryGetValue((org.Id, spec.Title), out var announcement))
                {
                    announcement.Content = spec.Content;
                    continue;
                }

                db.Announcements.Add(new Announcement
                {
                    OrganisationId = org.Id,
                    Title = spec.Title,
                    Content = spec.Content,
                    CreatedById = leader.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i * 6 + orgIndex)),
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // Invite history: accepted invites behind existing memberships, plus pending and declined
    // invites for the org-less tail of the roster so the accept/decline flow has live data.
    private async Task SeedOrganisationInvitesAsync(Dictionary<string, User> users, Organisation[] orgs, CancellationToken ct)
    {
        var userIds = UserSpecs.Select(s => users[s.Nickname].Id).ToArray();
        var existing = await db.OrganisationInvites
            .Where(i => userIds.Contains(i.InvitedUserId))
            .ToDictionaryAsync(i => (i.InvitedUserId, i.OrganisationId), ct);

        var orglessIndex = 0;

        for (var i = 0; i < UserSpecs.Length; i++)
        {
            var spec = UserSpecs[i];
            var user = users[spec.Nickname];

            if (spec.OrgIndex is int orgIndex)
            {
                // Members who are not the org's own leader carry an accepted invite as history.
                if (OrgSpecs[orgIndex].LeaderNickname == spec.Nickname) continue;

                AddInvite(orgIndex, OrganisationInviteStatus.Accepted, i);
                continue;
            }

            // Org-less user: one live pending invite, and (for every other one) a declined
            // invite from a different organisation.
            AddInvite(orglessIndex % OrgCount, OrganisationInviteStatus.Pending, i);
            if (orglessIndex % 2 == 1)
                AddInvite((orglessIndex + 2) % OrgCount, OrganisationInviteStatus.Declined, i);
            orglessIndex++;

            continue;

            void AddInvite(int targetOrgIndex, OrganisationInviteStatus status, int seedIndex)
            {
                var org = orgs[targetOrgIndex];
                var invitedBy = users[OrgSpecs[targetOrgIndex].LeaderNickname];
                var createdAt = DateTime.UtcNow.AddDays(-(30 + seedIndex));

                if (existing.TryGetValue((user.Id, org.Id), out var invite))
                {
                    invite.Status = status;
                    invite.AcceptedAt = status == OrganisationInviteStatus.Accepted ? invite.AcceptedAt ?? createdAt : null;
                    invite.DeclinedAt = status == OrganisationInviteStatus.Declined ? invite.DeclinedAt ?? createdAt : null;
                    return;
                }

                invite = new OrganisationInvite
                {
                    OrganisationId = org.Id,
                    InvitedUserId = user.Id,
                    InvitedById = invitedBy.Id,
                    Status = status,
                    AcceptedAt = status == OrganisationInviteStatus.Accepted ? createdAt : null,
                    DeclinedAt = status == OrganisationInviteStatus.Declined ? createdAt : null,
                    CreatedAt = createdAt,
                };
                db.OrganisationInvites.Add(invite);
                existing[(user.Id, org.Id)] = invite;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // Admin is wired to every user row in the database (both directions), so a freshly registered
    // test account also shows up in admin's feed on the next pass. On top of that, seed users
    // follow each other densely and across organisations, so /api/feed is never a single org's content.
    private async Task SeedFollowersAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        var adminId = users[AdminNickname].Id;
        var allUserIds = await db.Users.Select(u => u.Id).OrderBy(id => id).ToListAsync(ct);
        var seedIds = UserSpecs.Select(s => users[s.Nickname].Id).ToArray();

        var wanted = new List<(int FollowerId, int FollowingId)>();
        var wantedSet = new HashSet<(int, int)>();

        void Want(int followerId, int followingId)
        {
            if (followerId == followingId) return; // followers has CHECK(follower_id <> following_id)
            if (wantedSet.Add((followerId, followingId)))
                wanted.Add((followerId, followingId));
        }

        foreach (var id in allUserIds)
        {
            Want(adminId, id);
            Want(id, adminId);
        }

        for (var i = 0; i < seedIds.Length; i++)
        {
            var rng = PrngFor(Stream.Followers, i);
            var followCount = rng.Next(6, 14);
            foreach (var target in PickDistinct(rng, seedIds.Length, followCount, exclude: i))
            {
                Want(seedIds[i], seedIds[target]);
                // Roughly two thirds of follows are reciprocated — mutual pairs make the demo
                // feeds symmetrical without turning the graph into a clique.
                if (rng.Chance(65)) Want(seedIds[target], seedIds[i]);
            }
        }

        var existing = (await db.Followers
                .Select(f => new { f.FollowerId, f.FollowingId })
                .ToListAsync(ct))
            .Select(f => (f.FollowerId, f.FollowingId))
            .ToHashSet();

        foreach (var (followerId, followingId) in wanted)
        {
            if (!existing.Add((followerId, followingId))) continue;

            db.Followers.Add(new Follower
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow.AddDays(-PrngFor(Stream.Followers, followerId, followingId).Next(1, 300)),
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<Challenge>> SeedChallengesAsync(List<EagleFeather> feathers, CancellationToken ct)
    {
        var titles = ChallengeSpecs.Select(s => s.Title).ToArray();
        var byTitle = await db.Challenges
            .Where(c => titles.Contains(c.Title))
            .ToDictionaryAsync(c => c.Title, ct);

        var challenges = new List<Challenge>(ChallengeSpecs.Length);

        for (var i = 0; i < ChallengeSpecs.Length; i++)
        {
            var spec = ChallengeSpecs[i];
            var validFrom = DateTime.UtcNow.AddDays(-spec.ValidFromDaysAgo);

            if (byTitle.TryGetValue(spec.Title, out var challenge))
            {
                challenge.Description = spec.Description;
                challenge.DifficultyLevel = spec.DifficultyLevel;
                challenges.Add(challenge);
                continue;
            }

            challenge = new Challenge
            {
                Title = spec.Title,
                Description = spec.Description,
                EagleFeatherId = feathers[i * 7 % feathers.Count].Id,
                DifficultyLevel = spec.DifficultyLevel,
                ValidFrom = validFrom,
                ValidTo = validFrom.AddDays(spec.ValidForDays),
                CreatedAt = validFrom,
            };
            db.Challenges.Add(challenge);
            challenges.Add(challenge);
        }

        await db.SaveChangesAsync(ct);
        return challenges;
    }

    private async Task SeedUserChallengesAsync(Dictionary<string, User> users, List<Challenge> challenges, CancellationToken ct)
    {
        if (challenges.Count == 0) return;

        var userIds = UserSpecs.Select(s => users[s.Nickname].Id).ToArray();
        var existing = (await db.UserChallenges
                .Where(uc => userIds.Contains(uc.UserId))
                .Select(uc => new { uc.UserId, uc.ChallengeId })
                .ToListAsync(ct))
            .Select(uc => (uc.UserId, uc.ChallengeId))
            .ToHashSet();

        foreach (var spec in UserSpecs)
        {
            var user = users[spec.Nickname];

            foreach (var challenge in challenges)
            {
                var rng = PrngFor(Stream.UserChallenges, user.Id, challenge.Id);
                if (!rng.Chance(ChallengePinPercent)) continue;
                if (!existing.Add((user.Id, challenge.Id))) continue;

                var pinnedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 90));
                db.UserChallenges.Add(new UserChallenge
                {
                    UserId = user.Id,
                    ChallengeId = challenge.Id,
                    PinnedAt = pinnedAt,
                    // A quarter of pins are already released, so the "currently pinned" filter
                    // (unpinned_at IS NULL) has something to actually filter out.
                    UnpinnedAt = rng.Chance(25) ? pinnedAt.AddDays(rng.Next(1, 20)) : null,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<UserEagleFeather>> SeedUserEagleFeathersAsync(
        Dictionary<string, User> users, List<EagleFeather> feathers, CancellationToken ct)
    {
        // Any Leader may moderate, regardless of the child's organisation, so approvals are
        // spread over all of them rather than always coming from the same account.
        var leaderIds = UserSpecs.Where(s => s.Role == UserRole.Leader).Select(s => users[s.Nickname].Id).ToArray();

        var userIds = UserSpecs.Select(s => users[s.Nickname].Id).ToArray();
        var existing = await db.UserEagleFeathers
            .Where(uef => userIds.Contains(uef.UserId))
            .ToDictionaryAsync(uef => (uef.UserId, uef.EagleFeatherId), ct);

        var result = new List<UserEagleFeather>();
        var pool = feathers.Take(FeatherPoolSize).ToList();

        foreach (var spec in UserSpecs)
        {
            var user = users[spec.Nickname];
            var attemptPercent = spec.Nickname == AdminNickname ? AdminFeatherAttemptPercent : FeatherAttemptPercent;

            // The unique key is (user, feather) with no is_grand_challenge — at most one attempt
            // per feather per user, which a per-pair draw satisfies by construction.
            foreach (var feather in pool)
            {
                var rng = PrngFor(Stream.UserEagleFeathers, user.Id, feather.Id);
                if (!rng.Chance(attemptPercent)) continue;

                var roll = rng.Next(100);
                var status = roll switch
                {
                    < 55 => EagleFeatherStatus.Approved,
                    < 85 => EagleFeatherStatus.Pending,
                    _ => EagleFeatherStatus.Rejected,
                };
                var isGrand = rng.Chance(20);
                var verifiedById = status == EagleFeatherStatus.Pending ? (int?)null : rng.Pick(leaderIds);
                var note = status == EagleFeatherStatus.Rejected
                    ? $"{ContentTag} Chybí fotodokumentace, prosím doplň a pošli znovu."
                    : null;

                if (!existing.TryGetValue((user.Id, feather.Id), out var uef))
                {
                    uef = new UserEagleFeather
                    {
                        UserId = user.Id,
                        EagleFeatherId = feather.Id,
                        IsGrandChallenge = isGrand,
                        CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(5, 150)),
                    };
                    db.UserEagleFeathers.Add(uef);
                    existing[(user.Id, feather.Id)] = uef;
                }

                uef.Status = status;
                uef.VerifiedById = verifiedById;
                uef.ModeratorNote = note;
                uef.EarnedAt = status == EagleFeatherStatus.Approved ? uef.EarnedAt ?? DateTime.UtcNow.AddDays(-rng.Next(1, 60)) : null;

                result.Add(uef);
            }
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    // One post per attempt (the natural key is the UEF id). Posts exist for approved, pending and
    // rejected attempts alike — rejecting an attempt does not remove its posts.
    private async Task<List<Post>> SeedPostsAsync(
        Dictionary<string, User> users, List<UserEagleFeather> uefs, List<EagleFeather> feathers,
        List<Challenge> challenges, CancellationToken ct)
    {
        var usersById = users.Values.ToDictionary(u => u.Id);
        var feathersById = feathers.ToDictionary(f => f.Id);
        var uefIds = uefs.Select(u => u.Id).ToArray();
        var existingPosts = await db.Posts
            .Where(p => uefIds.Contains(p.UserEagleFeatherId))
            .ToDictionaryAsync(p => p.UserEagleFeatherId, ct);

        var posts = new List<Post>();

        foreach (var uef in uefs)
        {
            var rng = PrngFor(Stream.Posts, uef.Id);
            var author = usersById[uef.UserId];

            if (!rng.Chance(85))
            {
                // No post yet: the attempt was started but nothing was documented.
                if (!existingPosts.ContainsKey(uef.Id)) uef.IsCompleted = false;
                continue;
            }

            // Roughly half of an author's posts go to their organisation feed, the rest stay
            // follower-only (organisation_id = null) — that difference is what /api/feed and
            // /api/organisations/{id}/feed are meant to show.
            var organisationId = author.OrganisationId is int orgId && rng.Chance(55) ? orgId : (int?)null;

            uef.IsCompleted = true;

            if (existingPosts.TryGetValue(uef.Id, out var existingPost))
            {
                existingPost.OrganisationId = organisationId;
                posts.Add(existingPost);
                continue;
            }

            var feather = feathersById[uef.EagleFeatherId];
            var body = string.Format(rng.Pick(PostTemplates), feather.Name);
            var post = new Post
            {
                UserId = author.Id,
                OrganisationId = organisationId,
                UserEagleFeatherId = uef.Id,
                ChallengeId = challenges.Count > 0 && rng.Chance(25) ? rng.Pick(challenges).Id : null,
                Content = $"{ContentTag} {body}",
                ImageUrl = rng.Chance(60) ? $"https://picsum.photos/seed/trilobit{uef.Id}/800/600" : null,
                CreatedAt = DateTime.UtcNow
                    .AddDays(-rng.Next(0, 120))
                    .AddHours(-rng.Next(0, 24))
                    .AddMinutes(-rng.Next(0, 60)),
            };
            db.Posts.Add(post);
            posts.Add(post);
        }

        await db.SaveChangesAsync(ct);
        return posts;
    }

    // Two waves: top-level comments on posts, then replies on those comments (polymorphic
    // commentable_type = Comments). Replies need their parent's generated id, hence the split save.
    private async Task<List<Comment>> SeedCommentsAsync(Dictionary<string, User> users, List<Post> posts, CancellationToken ct)
    {
        if (posts.Count == 0) return [];

        var usersById = users.Values.ToDictionary(u => u.Id);
        var seedUsers = UserSpecs.Select(s => users[s.Nickname]).ToArray();
        var postIds = posts.Select(p => p.Id).ToArray();

        var existingOnPosts = (await db.Comments
                .Where(c => c.CommentableType == CommentableType.Posts && postIds.Contains(c.CommentableId))
                .ToListAsync(ct))
            .ToDictionary(c => (c.CommentableId, c.UserId));

        var topLevel = new List<Comment>();

        foreach (var post in posts)
        {
            var rng = PrngFor(Stream.Comments, post.Id);
            var author = usersById[post.UserId];
            var authorIndex = Array.FindIndex(seedUsers, u => u.Id == author.Id);

            // One comment per (post, commenter) — that pair is the natural key this seeder
            // dedupes on, so commenters must be distinct within a post.
            foreach (var commenterIndex in PickDistinct(rng, seedUsers.Length, rng.Next(0, 6), exclude: authorIndex))
            {
                var commenter = seedUsers[commenterIndex];
                if (existingOnPosts.TryGetValue((post.Id, commenter.Id), out var existing))
                {
                    topLevel.Add(existing);
                    continue;
                }

                var comment = new Comment
                {
                    UserId = commenter.Id,
                    CommentableType = CommentableType.Posts,
                    CommentableId = post.Id,
                    Content = $"{ContentTag} {string.Format(rng.Pick(CommentTemplates), author.FirstName)}",
                    CreatedAt = NotInFuture(post.CreatedAt.AddHours(rng.Next(1, 72))),
                };
                db.Comments.Add(comment);
                existingOnPosts[(post.Id, commenter.Id)] = comment;
                topLevel.Add(comment);
            }
        }

        await db.SaveChangesAsync(ct);

        var parentIds = topLevel.Select(c => c.Id).ToArray();
        var existingReplies = (await db.Comments
                .Where(c => c.CommentableType == CommentableType.Comments && parentIds.Contains(c.CommentableId))
                .ToListAsync(ct))
            .ToDictionary(c => (c.CommentableId, c.UserId));

        var replies = new List<Comment>();

        foreach (var parent in topLevel)
        {
            var rng = PrngFor(Stream.Replies, parent.Id);
            if (!rng.Chance(35)) continue;

            var parentAuthorIndex = Array.FindIndex(seedUsers, u => u.Id == parent.UserId);

            foreach (var replierIndex in PickDistinct(rng, seedUsers.Length, rng.Next(1, 3), exclude: parentAuthorIndex))
            {
                var replier = seedUsers[replierIndex];
                if (existingReplies.TryGetValue((parent.Id, replier.Id), out var existing))
                {
                    replies.Add(existing);
                    continue;
                }

                var reply = new Comment
                {
                    UserId = replier.Id,
                    CommentableType = CommentableType.Comments,
                    CommentableId = parent.Id,
                    Content = $"{ContentTag} {rng.Pick(ReplyTemplates)}",
                    CreatedAt = NotInFuture(parent.CreatedAt.AddHours(rng.Next(1, 48))),
                };
                db.Comments.Add(reply);
                existingReplies[(parent.Id, replier.Id)] = reply;
                replies.Add(reply);
            }
        }

        await db.SaveChangesAsync(ct);

        return [.. topLevel, .. replies];
    }

    private async Task SeedLikesAsync(Dictionary<string, User> users, List<Post> posts, List<Comment> comments, CancellationToken ct)
    {
        if (posts.Count == 0) return;

        var seedUsers = UserSpecs.Select(s => users[s.Nickname]).ToArray();
        var postIds = posts.Select(p => p.Id).ToArray();
        var commentIds = comments.Select(c => c.Id).ToArray();

        var existing = (await db.Likes
                .Where(l => (l.LikeableType == LikeableType.Posts && postIds.Contains(l.LikeableId))
                            || (l.LikeableType == LikeableType.Comments && commentIds.Contains(l.LikeableId)))
                .Select(l => new { l.UserId, l.LikeableType, l.LikeableId })
                .ToListAsync(ct))
            .Select(l => (l.UserId, l.LikeableType, l.LikeableId))
            .ToHashSet();

        void AddLikes(LikeableType type, int targetId, int authorId, DateTime targetCreatedAt, Prng rng, int maxLikes)
        {
            var authorIndex = Array.FindIndex(seedUsers, u => u.Id == authorId);
            foreach (var likerIndex in PickDistinct(rng, seedUsers.Length, rng.Next(0, maxLikes), exclude: authorIndex))
            {
                var liker = seedUsers[likerIndex];
                if (!existing.Add((liker.Id, type, targetId))) continue;

                db.Likes.Add(new Like
                {
                    UserId = liker.Id,
                    LikeableType = type,
                    LikeableId = targetId,
                    CreatedAt = NotInFuture(targetCreatedAt.AddHours(rng.Next(1, 96))),
                });
            }
        }

        foreach (var post in posts)
            AddLikes(LikeableType.Posts, post.Id, post.UserId, post.CreatedAt, PrngFor(Stream.PostLikes, post.Id), maxLikes: 16);

        foreach (var comment in comments)
            AddLikes(LikeableType.Comments, comment.Id, comment.UserId, comment.CreatedAt, PrngFor(Stream.CommentLikes, comment.Id), maxLikes: 5);

        await db.SaveChangesAsync(ct);
    }
}
