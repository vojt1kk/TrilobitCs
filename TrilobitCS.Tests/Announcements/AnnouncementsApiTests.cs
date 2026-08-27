using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Announcements;

[Collection("Api")]
public class AnnouncementsApiTests : ApiTestBase
{
    public AnnouncementsApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // =====================
    // POST /api/organisations/{id}/announcements
    // =====================

    [Fact]
    public async Task Create_Returns201_AsLeader()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);

        var response = await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("organisationId").GetInt32().Should().Be(orgId);
    }

    [Fact]
    public async Task Create_Returns403_AsNonLeader()
    {
        var (_, orgId) = await CreateOrganisationAsLeader();
        var memberToken = await RegisterAndGetToken();
        SetAuth(memberToken);

        var response = await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns403_AsLeaderOfDifferentOrganisation()
    {
        var (_, orgId) = await CreateOrganisationAsLeader();
        var (otherLeaderToken, _) = await CreateOrganisationAsLeader();
        SetAuth(otherLeaderToken);

        var response = await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns404_WhenOrganisationDoesNotExist()
    {
        var token = await RegisterLeaderAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsJsonAsync("/api/organisations/999999/announcements", CreateAnnouncementRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns422_WhenTitleEmpty()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);

        var response = await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements",
            new { title = "", content = "some content" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/organisations/1/announcements", CreateAnnouncementRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/organisations/{id}/announcements
    // =====================

    [Fact]
    public async Task GetAnnouncements_Returns200_AsMember()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        var response = await _client.GetAsync($"/api/organisations/{orgId}/announcements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetAnnouncements_Returns403_AsNonMember()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        var outsiderToken = await RegisterAndGetToken();
        SetAuth(outsiderToken);

        var response = await _client.GetAsync($"/api/organisations/{orgId}/announcements");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAnnouncements_Returns404_WhenOrganisationDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/organisations/999999/announcements");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAnnouncements_RespectsPagination()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync($"/api/organisations/{orgId}/announcements", CreateAnnouncementRequestFactory.Make());

        var response = await _client.GetAsync($"/api/organisations/{orgId}/announcements?page=1&pageSize=2");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(2);
        body.GetProperty("totalCount").GetInt32().Should().Be(3);
        body.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
    }

    // =====================
    // PUT /api/announcements/{id}
    // =====================

    [Fact]
    public async Task Update_Returns200_AsLeader()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var announcementId = await CreateAnnouncementAndGetId(orgId);

        var response = await _client.PutAsJsonAsync($"/api/announcements/{announcementId}", new { title = "Updated title", content = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Updated title");
        body.GetProperty("updatedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_Returns403_AsNonLeader()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var announcementId = await CreateAnnouncementAndGetId(orgId);

        var memberToken = await RegisterAndGetToken();
        SetAuth(memberToken);
        var response = await _client.PutAsJsonAsync($"/api/announcements/{announcementId}", new { title = "Hacked", content = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Returns404_WhenAnnouncementDoesNotExist()
    {
        var token = await RegisterLeaderAndGetToken();
        SetAuth(token);

        var response = await _client.PutAsJsonAsync("/api/announcements/999999", new { title = "x", content = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // DELETE /api/announcements/{id}
    // =====================

    [Fact]
    public async Task Delete_Returns204_AsLeader()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var announcementId = await CreateAnnouncementAndGetId(orgId);

        var response = await _client.DeleteAsync($"/api/announcements/{announcementId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Returns403_AsNonLeader()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        SetAuth(leaderToken);
        var announcementId = await CreateAnnouncementAndGetId(orgId);

        var memberToken = await RegisterAndGetToken();
        SetAuth(memberToken);
        var response = await _client.DeleteAsync($"/api/announcements/{announcementId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns404_WhenAnnouncementDoesNotExist()
    {
        var token = await RegisterLeaderAndGetToken();
        SetAuth(token);

        var response = await _client.DeleteAsync("/api/announcements/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> CreateAnnouncementAndGetId(int organisationId)
    {
        var response = await _client.PostAsJsonAsync($"/api/organisations/{organisationId}/announcements", CreateAnnouncementRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}
