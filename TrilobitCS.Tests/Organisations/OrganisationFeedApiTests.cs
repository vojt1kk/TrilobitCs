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

namespace TrilobitCS.Tests.Organisations;

[Collection("Api")]
public class OrganisationFeedApiTests : ApiTestBase
{
    public OrganisationFeedApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Shared container across tests → eagle feathers must be unique on (Light, Section, Number).
    // "OFT" prefix keeps this class's sequence from colliding with other test classes' seeded feathers.
    private static int _featherSeq;

    // =====================
    // GET /api/organisations/{id}/feed
    // =====================

    [Fact]
    public async Task GetOrganisationFeed_Returns200_WithOrgScopedPosts()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var postId = await SeedPostAsync(orgId);

        var response = await _client.GetAsync($"/api/organisations/{orgId}/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Contain(postId);
    }

    [Fact]
    public async Task GetOrganisationFeed_ExcludesPostsFromOtherOrganisations()
    {
        var (leaderAToken, orgAId) = await CreateOrganisationAsLeader();
        SetAuth(leaderAToken);
        var postInOrgA = await SeedPostAsync(orgAId);

        var (_, orgBId) = await CreateOrganisationAsLeader();

        var response = await _client.GetAsync($"/api/organisations/{orgBId}/feed");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().NotContain(postInOrgA);
    }

    [Fact]
    public async Task GetOrganisationFeed_AccessibleToNonMember()
    {
        // Deliberately no membership check on org feed (confirmed spec decision, asymmetric with announcements).
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var postId = await SeedPostAsync(orgId);

        var outsiderToken = await RegisterAndGetToken();
        SetAuth(outsiderToken);

        var response = await _client.GetAsync($"/api/organisations/{orgId}/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Contain(postId);
    }

    [Fact]
    public async Task GetOrganisationFeed_Returns404_WhenOrganisationDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/organisations/999999/feed");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrganisationFeed_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/organisations/1/feed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
            Section = $"OFT{seq}",
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
