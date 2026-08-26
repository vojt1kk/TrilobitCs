using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Requests;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Followers;

[Collection("Api")]
public class FollowersApiTests : ApiTestBase
{
    public FollowersApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // =====================
    // POST /api/users/{id}/follow
    // =====================

    [Fact]
    public async Task Follow_CreatesRow_Returns200()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);

        var response = await _client.PostAsync($"/api/users/{targetId}/follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("followingId").GetInt32().Should().Be(targetId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Followers.CountAsync(f => f.FollowingId == targetId)).Should().Be(1);
    }

    [Fact]
    public async Task Follow_Repeat_IsIdempotentReturns200SameRow()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);

        var first = await _client.PostAsync($"/api/users/{targetId}/follow", null);
        var second = await _client.PostAsync($"/api/users/{targetId}/follow", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Followers.CountAsync(f => f.FollowingId == targetId)).Should().Be(1);
    }

    [Fact]
    public async Task Follow_Repeat_Concurrent_BothReturn200AndExactlyOneRowExists()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);

        var task1 = _client.PostAsync($"/api/users/{targetId}/follow", null);
        var task2 = _client.PostAsync($"/api/users/{targetId}/follow", null);
        var responses = await Task.WhenAll(task1, task2);

        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Followers.CountAsync(f => f.FollowingId == targetId)).Should().Be(1);
    }

    [Fact]
    public async Task Follow_Self_Returns422()
    {
        var (token, id) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/users/{id}/follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.cannot_follow_self");
    }

    [Fact]
    public async Task Follow_NonexistentUser_Returns404()
    {
        var (token, _) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/users/999999/follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Follow_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/users/1/follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // DELETE /api/users/{id}/follow
    // =====================

    [Fact]
    public async Task Unfollow_Returns204AndRemovesRow()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);
        await _client.PostAsync($"/api/users/{targetId}/follow", null);

        var response = await _client.DeleteAsync($"/api/users/{targetId}/follow");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Followers.CountAsync(f => f.FollowingId == targetId)).Should().Be(0);
    }

    [Fact]
    public async Task Unfollow_NotFollowing_Returns204NoOp()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);

        var response = await _client.DeleteAsync($"/api/users/{targetId}/follow");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unfollow_Concurrent_BothReturn204AndNoRowRemains()
    {
        var (followerToken, _) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);
        await _client.PostAsync($"/api/users/{targetId}/follow", null);

        var task1 = _client.DeleteAsync($"/api/users/{targetId}/follow");
        var task2 = _client.DeleteAsync($"/api/users/{targetId}/follow");
        var responses = await Task.WhenAll(task1, task2);

        responses[0].StatusCode.Should().Be(HttpStatusCode.NoContent);
        responses[1].StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Followers.CountAsync(f => f.FollowingId == targetId)).Should().Be(0);
    }

    // =====================
    // GET /api/users/{id}/followers, /api/users/{id}/following
    // =====================

    [Fact]
    public async Task GetFollowers_WithRealData_ReturnsCorrectPaginatedList()
    {
        var (followerToken, followerId) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);
        await _client.PostAsync($"/api/users/{targetId}/follow", null);

        var response = await _client.GetAsync($"/api/users/{targetId}/followers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
        var items = body.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("id").GetInt32().Should().Be(followerId);
    }

    [Fact]
    public async Task GetFollowing_WithRealData_ReturnsCorrectPaginatedList()
    {
        var (followerToken, followerId) = await RegisterUser();
        var (_, targetId) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", followerToken);
        await _client.PostAsync($"/api/users/{targetId}/follow", null);

        var response = await _client.GetAsync($"/api/users/{followerId}/following");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
        var items = body.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("id").GetInt32().Should().Be(targetId);
    }

    private async Task<(string AccessToken, int UserId)> RegisterUser(RegisterRequest? request = null)
    {
        var req = request ?? RegisterRequestFactory.Make();
        var response = await _client.PostAsJsonAsync("/api/auth/register", req);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString()!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == req.Email);

        return (accessToken, user.Id);
    }

    [Fact]
    public async Task GetFollowers_NonexistentUser_Returns404()
    {
        var (token, _) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/999999/followers");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFollowing_NonexistentUser_Returns404()
    {
        var (token, _) = await RegisterUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/999999/following");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
