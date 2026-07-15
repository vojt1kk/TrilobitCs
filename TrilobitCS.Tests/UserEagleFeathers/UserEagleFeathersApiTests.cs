using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.UserEagleFeathers;

[Collection("Api")]
public class UserEagleFeathersApiTests : ApiTestBase
{
    public UserEagleFeathersApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Shared container across tests → eagle feathers must be unique on (Light, Section, Number).
    private static int _featherSeq;

    // Status enum is serialized as int: Pending=0, Approved=1, Rejected=2.
    private const int StatusPending = 0;
    private const int StatusApproved = 1;
    private const int StatusRejected = 2;

    // =====================
    // POST /api/user-eagle-feathers
    // =====================

    [Fact]
    public async Task Create_Returns201_WithValidRequest()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();

        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(featherId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(StatusPending);
        body.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        body.GetProperty("eagleFeatherId").GetInt32().Should().Be(featherId);
    }

    [Fact]
    public async Task Create_Returns422_WhenDuplicate()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();

        await _client.PostAsJsonAsync("/api/user-eagle-feathers", CreateUserEagleFeatherRequestFactory.Make(featherId));
        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers", CreateUserEagleFeatherRequestFactory.Make(featherId));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_eagle_feather_already_exists");
    }

    [Fact]
    public async Task Create_Returns404_WhenEagleFeatherDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(999999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.eagle_feather_not_found");
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/user/eagle-feathers
    // =====================

    [Fact]
    public async Task GetMine_Returns200_WithMyUefs()
    {
        var tokenA = await RegisterAndGetToken();
        var tokenB = await RegisterAndGetToken();

        SetAuth(tokenA);
        var feather1 = await SeedEagleFeatherAsync();
        var feather2 = await SeedEagleFeatherAsync();
        await CreateUefAndGetId(feather1);
        await CreateUefAndGetId(feather2);

        SetAuth(tokenB);
        var feather3 = await SeedEagleFeatherAsync();
        await CreateUefAndGetId(feather3);

        SetAuth(tokenA);
        var response = await _client.GetAsync("/api/user/eagle-feathers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(2);
        body.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetMine_Returns401_WhenUnauthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/user/eagle-feathers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // DELETE /api/user-eagle-feathers/{id}
    // =====================

    [Fact]
    public async Task Delete_Returns204_AndCascadesPost()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        // Attach a post via the existing endpoint.
        var postResp = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/posts",
            new { content = "my achievement", imageUrl = (string?)null, organisationId = (int?)null, challengeId = (int?)null });
        postResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var postId = (await postResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var response = await _client.DeleteAsync($"/api/user-eagle-feathers/{uefId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.UserEagleFeathers.AnyAsync(u => u.Id == uefId)).Should().BeFalse();
        (await db.Posts.AnyAsync(p => p.Id == postId)).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Returns403_WhenNotOwner()
    {
        var ownerToken = await RegisterAndGetToken();
        SetAuth(ownerToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var otherToken = await RegisterAndGetToken();
        SetAuth(otherToken);
        var response = await _client.DeleteAsync($"/api/user-eagle-feathers/{uefId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns404_WhenUefDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.DeleteAsync("/api/user-eagle-feathers/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_WhenStatusApproved()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        await SetUefStatusAsync(uefId, EagleFeatherStatus.Approved);

        var response = await _client.DeleteAsync($"/api/user-eagle-feathers/{uefId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =====================
    // POST /api/user-eagle-feathers/{id}/retry
    // =====================

    [Fact]
    public async Task Retry_Returns201_FromRejected()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        await SetUefStatusAsync(uefId, EagleFeatherStatus.Rejected, verifiedById: null, note: "needs more photos");

        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/retry", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(StatusPending);
        body.GetProperty("verifiedById").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("moderatorNote").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("earnedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Retry_Returns422_FromPending()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/retry", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_eagle_feather_cannot_retry");
    }

    [Fact]
    public async Task Retry_Returns422_FromApproved()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        await SetUefStatusAsync(uefId, EagleFeatherStatus.Approved);

        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/retry", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Retry_Returns403_WhenNotOwner()
    {
        var ownerToken = await RegisterAndGetToken();
        SetAuth(ownerToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        await SetUefStatusAsync(uefId, EagleFeatherStatus.Rejected);

        var otherToken = await RegisterAndGetToken();
        SetAuth(otherToken);
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/retry", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =====================
    // GET /api/user-eagle-feathers/pending
    // =====================

    [Fact]
    public async Task GetPending_Returns200_AsLeader_ShowsAllPending()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        await CreateUefAndGetId(featherId);

        var leaderToken = await RegisterLeaderAndGetToken();
        SetAuth(leaderToken);
        var response = await _client.GetAsync("/api/user-eagle-feathers/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetPending_Returns403_AsNonLeader()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/user-eagle-feathers/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.leader_only");
    }

    // =====================
    // POST /api/user-eagle-feathers/{id}/approve
    // =====================

    [Fact]
    public async Task Approve_Returns201_AndSetsEarnedAtAndNote()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var leaderToken = await RegisterLeaderAndGetToken();
        var leaderId = await GetUserIdFromToken(leaderToken);
        SetAuth(leaderToken);
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/approve", new { note = "well done" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(StatusApproved);
        body.GetProperty("earnedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.GetProperty("verifiedById").GetInt32().Should().Be(leaderId);
        body.GetProperty("moderatorNote").GetString().Should().Be("well done");
    }

    [Fact]
    public async Task Approve_Returns422_WhenAlreadyApproved()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var leaderToken = await RegisterLeaderAndGetToken();
        SetAuth(leaderToken);
        await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/approve", new { note = (string?)null });
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/approve", new { note = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_eagle_feather_already_moderated");
    }

    [Fact]
    public async Task Approve_Returns403_AsNonLeader()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/approve", new { note = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =====================
    // POST /api/user-eagle-feathers/{id}/reject
    // =====================

    [Fact]
    public async Task Reject_Returns201_AndSetsNote_ButNotEarnedAt()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var leaderToken = await RegisterLeaderAndGetToken();
        SetAuth(leaderToken);
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/reject", new { note = "not enough evidence" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(StatusRejected);
        body.GetProperty("earnedAt").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("moderatorNote").GetString().Should().Be("not enough evidence");
    }

    [Fact]
    public async Task Reject_Returns422_WhenAlreadyRejected()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var leaderToken = await RegisterLeaderAndGetToken();
        SetAuth(leaderToken);
        await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/reject", new { note = (string?)null });
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/reject", new { note = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Reject_DoesNotDeletePosts()
    {
        var childToken = await RegisterAndGetToken();
        SetAuth(childToken);
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);

        var postResp = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/posts",
            new { content = "evidence", imageUrl = (string?)null, organisationId = (int?)null, challengeId = (int?)null });
        var postId = (await postResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var leaderToken = await RegisterLeaderAndGetToken();
        SetAuth(leaderToken);
        await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/reject", new { note = (string?)null });

        SetAuth(childToken);
        var getPost = await _client.GetAsync($"/api/posts/{postId}");
        getPost.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =====================
    // Helpers
    // =====================

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> SeedEagleFeatherAsync()
    {
        var seq = Interlocked.Increment(ref _featherSeq);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ef = new EagleFeather
        {
            Light = (byte)((seq % 4) + 1),
            Section = $"T{seq}",
            Number = (short)seq,
            Name = "Test pero",
            Challenge = "cin",
            GrandChallenge = "velky cin",
            SourceUrl = "https://example.com",
        };
        db.EagleFeathers.Add(ef);
        await db.SaveChangesAsync();
        return ef.Id;
    }

    private async Task<int> CreateUefAndGetId(int eagleFeatherId, bool isGrandChallenge = false)
    {
        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(eagleFeatherId, isGrandChallenge));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task SetUefStatusAsync(int uefId, EagleFeatherStatus status, int? verifiedById = null, string? note = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uef = await db.UserEagleFeathers.FindAsync(uefId);
        uef!.Status = status;
        uef.VerifiedById = verifiedById;
        uef.ModeratorNote = note;
        uef.EarnedAt = status == EagleFeatherStatus.Approved ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
    }

    private async Task<int> GetUserIdFromToken(string token)
    {
        var previous = _client.DefaultRequestHeaders.Authorization;
        SetAuth(token);
        var response = await _client.GetAsync("/api/user/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = previous;
        return body.GetProperty("id").GetInt32();
    }
}
