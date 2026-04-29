using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Organisations;

[Collection("Api")]
public class OrganisationApiTests
{
    private readonly TrilobitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganisationApiTests(TrilobitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================
    // POST /api/organisations
    // =====================

    [Fact]
    public async Task CreateOrganisation_AsLeader_Returns200()
    {
        var accessToken = await RegisterLeaderAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("memberCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CreateOrganisation_SetsLeaderOrganisationId()
    {
        var registerRequest = RegisterRequestFactory.Make();
        var accessToken = await RegisterLeaderAndGetToken(registerRequest);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == registerRequest.Email);
        user.OrganisationId.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrganisation_AsRegularUser_Returns403()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrganisation_LeaderAlreadyHasOrg_Returns422()
    {
        var accessToken = await RegisterLeaderAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());
        var response = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.organisation_already_exists");
    }

    [Fact]
    public async Task CreateOrganisation_InvalidData_Returns422()
    {
        var accessToken = await RegisterLeaderAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/organisations", new { Name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateOrganisation_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/organisations/{id}
    // =====================

    [Fact]
    public async Task GetOrganisation_Authenticated_Returns200()
    {
        var (accessToken, orgId) = await CreateOrganisationAsLeader();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/organisations/{orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(orgId);
        body.GetProperty("leader").GetProperty("nickname").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrganisation_NotFound_Returns404()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/organisations/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.organisation_not_found");
    }

    [Fact]
    public async Task GetOrganisation_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/organisations/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/organisations/{id}/members
    // =====================

    [Fact]
    public async Task GetOrganisationMembers_Returns200WithMemberList()
    {
        var (accessToken, orgId) = await CreateOrganisationAsLeader();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/organisations/{orgId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetOrganisationMembers_NotFound_Returns404()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/organisations/999999/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // DELETE /api/user/organisation
    // =====================

    [Fact]
    public async Task LeaveOrganisation_Member_Returns204AndClearsOrganisationId()
    {
        var (_, memberToken, memberEmail) = await SetupOrgWithMember();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await _client.DeleteAsync("/api/user/organisation");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == memberEmail);
        user.OrganisationId.Should().BeNull();
    }

    [Fact]
    public async Task LeaveOrganisation_Leader_Returns422()
    {
        var (leaderToken, _, _) = await SetupOrgWithMember();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);

        var response = await _client.DeleteAsync("/api/user/organisation");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.leader_cannot_leave");
    }

    [Fact]
    public async Task LeaveOrganisation_NotInOrg_Returns422()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.DeleteAsync("/api/user/organisation");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.not_in_organisation");
    }

    [Fact]
    public async Task LeaveOrganisation_Unauthenticated_Returns401()
    {
        var response = await _client.DeleteAsync("/api/user/organisation");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // Helpers
    // =====================

    private async Task<string> RegisterAndGetToken(RegisterRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request ?? RegisterRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> RegisterLeaderAndGetToken(RegisterRequest? request = null)
    {
        var req = request ?? RegisterRequestFactory.Make();
        var response = await _client.PostAsJsonAsync("/api/auth/register", req);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString()!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == req.Email);
        user.Role = UserRole.Leader;
        await db.SaveChangesAsync();

        return accessToken;
    }

    private async Task<(string AccessToken, int OrgId)> CreateOrganisationAsLeader()
    {
        var accessToken = await RegisterLeaderAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var orgResponse = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());
        var orgBody = await orgResponse.Content.ReadFromJsonAsync<JsonElement>();
        return (accessToken, orgBody.GetProperty("id").GetInt32());
    }

    private async Task<(string LeaderToken, string MemberToken, string MemberEmail)> SetupOrgWithMember()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();

        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteResponse = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = memberReq.Nickname });
        var inviteId = (await inviteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/accept", new { });

        return (leaderToken, memberToken, memberReq.Email);
    }
}
