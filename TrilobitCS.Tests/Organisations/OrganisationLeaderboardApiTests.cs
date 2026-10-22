using System.Net;
using FluentAssertions;
using TrilobitCS.Tests.Infrastructure;
using Xunit;

namespace TrilobitCS.Tests.Organisations;

[Collection("Api")]
public class OrganisationLeaderboardApiTests : SocialApiTestBase
{
    public OrganisationLeaderboardApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Org with leader (0 feathers), top (2 approved + 1 pending), mid (1 approved + 1 rejected).
    private async Task<(TestUser Leader, TestUser Top, TestUser Mid, int OrgId)> SeedOrgAsync()
    {
        var (leader, orgId) = await CreateLeaderWithOrganisationAsync();
        var top = await CreateUserAsync(organisationId: orgId);
        var mid = await CreateUserAsync(organisationId: orgId);

        await ApproveUefAsync(await CreateUefAsync(top), leader);
        await ApproveUefAsync(await CreateUefAsync(top), leader);
        await CreateUefAsync(top); // pending, must not count

        await ApproveUefAsync(await CreateUefAsync(mid), leader);
        var rejectedUef = await CreateUefAsync(mid);
        (await PostAsync($"/api/user-eagle-feathers/{rejectedUef}/reject", leader.Token, new { note = (string?)null }))
            .EnsureSuccessStatusCode();

        return (leader, top, mid, orgId);
    }

    [Fact]
    public async Task GetLeaderboard_OrdersByApprovedCount_AndIncludesMembersWithZero()
    {
        var (leader, top, mid, orgId) = await SeedOrgAsync();

        var response = await GetAsync($"/api/organisations/{orgId}/leaderboard", leader.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await JsonAsync(response)).GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("userId").GetInt32()).Should().Equal(top.Id, mid.Id, leader.Id);
        items.Select(i => i.GetProperty("approvedFeatherCount").GetInt32()).Should().Equal(2, 1, 0);
        items.Select(i => i.GetProperty("rank").GetInt32()).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task GetLeaderboard_CountsOnlyApprovedFeathers()
    {
        var (leader, top, _, orgId) = await SeedOrgAsync();

        var response = await GetAsync($"/api/organisations/{orgId}/leaderboard", leader.Token);

        var topEntry = (await JsonAsync(response)).GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("userId").GetInt32() == top.Id);
        topEntry.GetProperty("approvedFeatherCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GetLeaderboard_RankContinuesAcrossPages()
    {
        var (leader, _, _, orgId) = await SeedOrgAsync();

        var response = await GetAsync($"/api/organisations/{orgId}/leaderboard?page=2&pageSize=2", leader.Token);

        var items = (await JsonAsync(response)).GetProperty("items").EnumerateArray().ToList();
        items.Should().ContainSingle();
        items[0].GetProperty("rank").GetInt32().Should().Be(3);
        items[0].GetProperty("userId").GetInt32().Should().Be(leader.Id);
    }

    [Fact]
    public async Task GetLeaderboard_ExcludesUsersFromOtherOrganisations()
    {
        var (leader, _, _, orgId) = await SeedOrgAsync();
        var (otherLeader, _) = await CreateLeaderWithOrganisationAsync();

        var response = await GetAsync($"/api/organisations/{orgId}/leaderboard", leader.Token);

        var ids = (await JsonAsync(response)).GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("userId").GetInt32());
        ids.Should().NotContain(otherLeader.Id);
    }

    [Fact]
    public async Task GetLeaderboard_AccessibleToNonMember()
    {
        var (_, _, _, orgId) = await SeedOrgAsync();
        var outsider = await CreateUserAsync();

        var response = await GetAsync($"/api/organisations/{orgId}/leaderboard", outsider.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLeaderboard_Returns404_WhenOrganisationDoesNotExist()
    {
        var user = await CreateUserAsync();

        var response = await GetAsync("/api/organisations/999999999/leaderboard", user.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.organisation_not_found");
    }

    [Fact]
    public async Task GetLeaderboard_Returns401_WhenUnauthenticated()
    {
        var response = await GetAsync("/api/organisations/1/leaderboard", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
