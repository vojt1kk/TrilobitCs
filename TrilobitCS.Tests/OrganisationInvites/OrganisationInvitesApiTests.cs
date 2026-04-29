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

namespace TrilobitCS.Tests.OrganisationInvites;

[Collection("Api")]
public class OrganisationInvitesApiTests
{
    private readonly TrilobitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganisationInvitesApiTests(TrilobitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================
    // POST /api/organisation-invites
    // =====================

    [Fact]
    public async Task Send_ByLeader_Returns200()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = memberReq.Nickname });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(0);
        body.GetProperty("invitedUserNickname").GetString().Should().Be(memberReq.Nickname);
    }

    [Fact]
    public async Task Send_TargetNotFound_Returns404()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);

        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = "nonexistent_user_xyz" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_not_found");
    }

    [Fact]
    public async Task Send_TargetAlreadyInOrg_Returns422()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();

        var otherLeaderReq = RegisterRequestFactory.Make();
        var otherLeaderToken = await RegisterLeaderAndGetToken(otherLeaderReq);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherLeaderToken);
        await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = otherLeaderReq.Nickname });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_already_in_organisation");
    }

    [Fact]
    public async Task Send_DuplicatePending_Returns422()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = memberReq.Nickname });
        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = memberReq.Nickname });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Send_NonLeader_Returns403()
    {
        var regularToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularToken);

        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = "anyone" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Send_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = "anyone" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/organisation-invites
    // =====================

    [Fact]
    public async Task GetInvites_ReturnsUserInvites_Returns200()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname = memberReq.Nickname });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var response = await _client.GetAsync("/api/organisation-invites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(1);
    }

    // =====================
    // POST /api/organisation-invites/{id}/accept
    // =====================

    [Fact]
    public async Task Accept_Returns200_SetsOrganisationId()
    {
        var (leaderToken, orgId) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteId = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var response = await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/accept", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == memberReq.Email);
        user.OrganisationId.Should().Be(orgId);
    }

    [Fact]
    public async Task Accept_AutoDeclinesOtherPending()
    {
        var (leader1Token, _) = await CreateOrganisationAsLeader();
        var (leader2Token, _) = await CreateOrganisationAsLeader();

        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leader1Token);
        var inviteId1 = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leader2Token);
        var inviteId2 = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId1}/accept", new { });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var declined = await db.OrganisationInvites.FindAsync(inviteId2);
        declined!.Status.Should().Be(OrganisationInviteStatus.Declined);
    }

    [Fact]
    public async Task Accept_AlreadyAccepted_Returns422()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteId = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/accept", new { });
        var response = await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/accept", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.invite_not_pending");
    }

    [Fact]
    public async Task Accept_WrongUser_Returns404()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteId = await SendInviteAndGetId(memberReq.Nickname);

        var otherToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/accept", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // POST /api/organisation-invites/{id}/decline
    // =====================

    [Fact]
    public async Task Decline_Returns200()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteId = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var response = await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/decline", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Decline_AlreadyDeclined_Returns422()
    {
        var (leaderToken, _) = await CreateOrganisationAsLeader();
        var memberReq = RegisterRequestFactory.Make();
        var memberToken = await RegisterAndGetToken(memberReq);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        var inviteId = await SendInviteAndGetId(memberReq.Nickname);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/decline", new { });
        var response = await _client.PostAsJsonAsync($"/api/organisation-invites/{inviteId}/decline", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.invite_not_pending");
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

    private async Task<int> SendInviteAndGetId(string nickname)
    {
        var response = await _client.PostAsJsonAsync("/api/organisation-invites", new { nickname });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}
