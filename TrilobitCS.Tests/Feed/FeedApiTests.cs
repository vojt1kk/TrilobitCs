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

namespace TrilobitCS.Tests.Feed;

[Collection("Api")]
public class FeedApiTests : ApiTestBase
{
    public FeedApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Shared container across tests → eagle feathers must be unique on (Light, Section, Number).
    // "FT" prefix keeps this class's sequence from colliding with other test classes' seeded feathers.
    private static int _featherSeq;

    // =====================
    // GET /api/feed
    // =====================

    [Fact]
    public async Task GetFeed_Returns200_WithOwnPosts()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var response = await _client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Contain(postId);
    }

    [Fact]
    public async Task GetFeed_IncludesPostsFromFollowedUsers()
    {
        var followedReq = RegisterRequestFactory.Make();
        var followedToken = await RegisterAndGetToken(followedReq);
        SetAuth(followedToken);
        var followedPostId = await SeedPostAsync();
        var followedUserId = await GetUserIdByEmailAsync(followedReq.Email);

        var followerToken = await RegisterAndGetToken();
        SetAuth(followerToken);
        await _client.PostAsync($"/api/users/{followedUserId}/follow", null);

        var response = await _client.GetAsync("/api/feed");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Contain(followedPostId);
    }

    [Fact]
    public async Task GetFeed_ExcludesPostsFromNonFollowedUsers()
    {
        var strangerToken = await RegisterAndGetToken();
        SetAuth(strangerToken);
        var strangerPostId = await SeedPostAsync();

        var viewerToken = await RegisterAndGetToken();
        SetAuth(viewerToken);

        var response = await _client.GetAsync("/api/feed");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().NotContain(strangerPostId);
    }

    [Fact]
    public async Task GetFeed_IncludesOrganisationScopedPostsFromFollowedUsers()
    {
        // Personal feed deliberately does NOT filter by OrganisationId (confirmed spec decision).
        var leaderReq = RegisterRequestFactory.Make();
        var leaderToken = await RegisterLeaderAndGetToken(leaderReq);
        SetAuth(leaderToken);
        var orgResponse = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());
        var orgId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var orgPostId = await SeedPostAsync(orgId);
        var leaderId = await GetUserIdByEmailAsync(leaderReq.Email);

        var followerToken = await RegisterAndGetToken();
        SetAuth(followerToken);
        await _client.PostAsync($"/api/users/{leaderId}/follow", null);

        var response = await _client.GetAsync("/api/feed");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Contain(orgPostId);
    }

    [Fact]
    public async Task GetFeed_Returns200_WithEmptyItems_WhenNoPostsOrFollows()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(0);
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetFeed_RespectsPagination()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        for (var i = 0; i < 3; i++)
            await SeedPostAsync();

        var response = await _client.GetAsync("/api/feed?page=1&pageSize=2");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(2);
        body.GetProperty("totalCount").GetInt32().Should().Be(3);
        body.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetFeed_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        return user.Id;
    }

    private async Task<int> SeedPostAsync(int? organisationId = null)
    {
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/posts",
            new { content = "achievement", imageUrl = (string?)null, organisationId, challengeId = (int?)null });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateUefAndGetId(int eagleFeatherId)
    {
        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(eagleFeatherId));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> SeedEagleFeatherAsync()
    {
        var seq = Interlocked.Increment(ref _featherSeq);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ef = new EagleFeather
        {
            Light = (byte)((seq % 4) + 1),
            Section = $"FT{seq}",
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
}
