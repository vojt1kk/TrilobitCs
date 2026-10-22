using System.Net;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Features.Challenges;
using TrilobitCS.Models;
using TrilobitCS.Tests.Infrastructure;
using Xunit;

namespace TrilobitCS.Tests.Challenges;

[Collection("Api")]
public class ChallengesApiTests : SocialApiTestBase
{
    public ChallengesApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Inserts a challenge. Defaults make it the newest active one (ValidFrom = now).
    private async Task<int> InsertChallengeAsync(DateTime? validFrom = null, DateTime? validTo = null)
    {
        var featherId = await SeedEagleFeatherAsync();
        var from = validFrom ?? DateTime.UtcNow;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = new Challenge
        {
            Title = "Test vyzva",
            Description = "popis",
            EagleFeatherId = featherId,
            DifficultyLevel = 2,
            ValidFrom = from,
            ValidTo = validTo ?? from.AddDays(7),
            CreatedAt = from,
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();
        return challenge.Id;
    }

    // =====================
    // GET /api/challenges/current
    // =====================

    [Fact]
    public async Task GetCurrent_ReturnsNewestActiveChallenge_WithPinState()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();

        var response = await GetAsync("/api/challenges/current", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("challenge").GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("isPinned").GetBoolean().Should().BeFalse();
        body.GetProperty("pinnedCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetCurrent_IgnoresExpiredAndFutureChallenges()
    {
        var user = await CreateUserAsync();
        var activeId = await InsertChallengeAsync();
        await InsertChallengeAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));
        await InsertChallengeAsync(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(37));

        var response = await GetAsync("/api/challenges/current", user.Token);

        var body = await JsonAsync(response);
        body.GetProperty("challenge").GetProperty("id").GetInt32().Should().Be(activeId);
    }

    [Fact]
    public async Task GetCurrent_Returns401_WhenUnauthenticated()
    {
        var response = await GetAsync("/api/challenges/current", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/challenges/{id}
    // =====================

    [Fact]
    public async Task GetChallenge_Returns200_ForExistingChallenge()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();

        var response = await GetAsync($"/api/challenges/{id}", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("title").GetString().Should().Be("Test vyzva");
    }

    [Fact]
    public async Task GetChallenge_Returns404_ForUnknownChallenge()
    {
        var user = await CreateUserAsync();

        var response = await GetAsync("/api/challenges/999999999", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.challenge_not_found");
    }

    // =====================
    // POST /api/challenges/{id}/pin and /unpin
    // =====================

    [Fact]
    public async Task Pin_Returns200_AndMarksChallengePinned()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();

        var response = await PostAsync($"/api/challenges/{id}/pin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("isPinned").GetBoolean().Should().BeTrue();
        body.GetProperty("pinnedCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Pin_CountsPinsFromAllUsers_ButIsPinnedOnlyForCaller()
    {
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();
        var id = await InsertChallengeAsync();
        await PostAsync($"/api/challenges/{id}/pin", first.Token);

        var response = await GetAsync("/api/challenges/current", second.Token);

        var body = await JsonAsync(response);
        body.GetProperty("challenge").GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("isPinned").GetBoolean().Should().BeFalse();
        body.GetProperty("pinnedCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Pin_Returns422_WhenAlreadyPinned()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();
        await PostAsync($"/api/challenges/{id}/pin", user.Token);

        var response = await PostAsync($"/api/challenges/{id}/pin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.challenge_already_pinned");
    }

    [Fact]
    public async Task Pin_ReusesRow_WhenRepinningAfterUnpin()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();
        await PostAsync($"/api/challenges/{id}/pin", user.Token);
        await PostAsync($"/api/challenges/{id}/unpin", user.Token);

        var response = await PostAsync($"/api/challenges/{id}/pin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(response)).GetProperty("isPinned").GetBoolean().Should().BeTrue();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.UserChallenges.Where(uc => uc.ChallengeId == id && uc.UserId == user.Id).ToListAsync();
        rows.Should().ContainSingle().Which.UnpinnedAt.Should().BeNull();
    }

    [Fact]
    public async Task Pin_Returns404_WhenChallengeIsExpired()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));

        var response = await PostAsync($"/api/challenges/{id}/pin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.challenge_not_found");
    }

    [Fact]
    public async Task Pin_Returns404_WhenChallengeDoesNotExist()
    {
        var user = await CreateUserAsync();

        var response = await PostAsync("/api/challenges/999999999/pin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unpin_Returns200_AndClearsPinState()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();
        await PostAsync($"/api/challenges/{id}/pin", user.Token);

        var response = await PostAsync($"/api/challenges/{id}/unpin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("isPinned").GetBoolean().Should().BeFalse();
        body.GetProperty("pinnedCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Unpin_Returns422_WhenNotPinned()
    {
        var user = await CreateUserAsync();
        var id = await InsertChallengeAsync();

        var response = await PostAsync($"/api/challenges/{id}/unpin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.challenge_not_pinned");
    }

    [Fact]
    public async Task Unpin_Returns404_WhenChallengeDoesNotExist()
    {
        var user = await CreateUserAsync();

        var response = await PostAsync("/api/challenges/999999999/unpin", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // GenerateWeeklyChallengeCommand (A5, A6)
    // =====================

    private async Task<int?> GenerateAsync(DateTime now)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GenerateWeeklyChallengeCommand(now));
    }

    [Fact]
    public async Task Generate_CreatesWeeklyChallengeFromCatalogFeather_AndIsIdempotentForSameNow()
    {
        await SeedEagleFeatherAsync();
        var now = DateTime.UtcNow.AddYears(10);

        var id = await GenerateAsync(now);

        id.Should().NotBeNull();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.Challenges.Include(c => c.EagleFeather).SingleAsync(c => c.Id == id);
            row.ValidFrom.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
            row.ValidTo.Should().BeCloseTo(now.AddDays(7), TimeSpan.FromMilliseconds(1));
            row.Title.Should().Be(row.EagleFeather.Name);
            row.Description.Should().Be(row.EagleFeather.Challenge);
            row.DifficultyLevel.Should().BeInRange(1, 3);
        }
        (await GenerateAsync(now)).Should().BeNull();
    }

    [Fact]
    public async Task Generate_ReturnsNull_WhenNewestActiveChallengeIsYoungerThanSevenDays()
    {
        await SeedEagleFeatherAsync();
        var x = DateTime.UtcNow.AddYears(20);
        await InsertChallengeAsync(x, x.AddDays(14));

        (await GenerateAsync(x.AddDays(6))).Should().BeNull();
    }

    [Fact]
    public async Task Generate_CreatesChallenge_WhenNewestActiveChallengeIsSevenDaysOld()
    {
        await SeedEagleFeatherAsync();
        var x = DateTime.UtcNow.AddYears(21);
        await InsertChallengeAsync(x, x.AddDays(14));

        var id = await GenerateAsync(x.AddDays(7));

        id.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePost_Returns404_WhenChallengeDoesNotExist()
    {
        var author = await CreateUserAsync();
        var uefId = await CreateUefAsync(author);

        var response = await PostAsync($"/api/user-eagle-feathers/{uefId}/posts", author.Token,
            new { content = "x", imageUrl = (string?)null, organisationId = (int?)null, challengeId = 999999 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.challenge_not_found");
    }
}
