using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Tests.Factories;

namespace TrilobitCS.Tests.Infrastructure;

public record TestUser(int Id, string Token, string Email);

// Helpers that always pass the bearer token per request (no shared HttpClient auth state),
// so one test can act as several users.
public abstract class SocialApiTestBase : ApiTestBase
{
    // Shared DB → eagle feathers must be unique on (Light, Section, Number).
    private static int _featherSeq;

    protected SocialApiTestBase(TrilobitWebApplicationFactory factory) : base(factory) { }

    protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    protected Task<HttpResponseMessage> GetAsync(string url, string? token) => SendAsync(HttpMethod.Get, url, token);
    protected Task<HttpResponseMessage> PostAsync(string url, string? token, object? body = null) => SendAsync(HttpMethod.Post, url, token, body);

    protected async Task<TestUser> CreateUserAsync(bool isPrivate = false, int? organisationId = null, bool leader = false)
    {
        var req = RegisterRequestFactory.Make();
        var token = await RegisterAndGetToken(req);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == req.Email);
        user.IsPrivate = isPrivate;
        user.OrganisationId = organisationId;
        if (leader) user.Role = UserRole.Leader;
        await db.SaveChangesAsync();
        return new TestUser(user.Id, token, req.Email);
    }

    protected async Task<(TestUser Leader, int OrgId)> CreateLeaderWithOrganisationAsync()
    {
        var leader = await CreateUserAsync(leader: true);
        var response = await PostAsync("/api/organisations", leader.Token, CreateOrganisationRequestFactory.Make());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (leader, body.GetProperty("id").GetInt32());
    }

    protected async Task FollowAsync(TestUser follower, TestUser target)
    {
        var response = await PostAsync($"/api/users/{target.Id}/follow", follower.Token);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<int> SeedEagleFeatherAsync()
    {
        var seq = Interlocked.Increment(ref _featherSeq);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ef = new EagleFeather
        {
            Light = (byte)((seq % 4) + 1),
            Section = $"SB{seq}",
            Number = (short)seq,
            Name = $"Social test pero {seq}",
            Challenge = "cin",
            GrandChallenge = "velky cin",
            SourceUrl = "https://example.com",
        };
        db.EagleFeathers.Add(ef);
        await db.SaveChangesAsync();
        return ef.Id;
    }

    protected async Task<int> CreateUefAsync(TestUser user)
    {
        var featherId = await SeedEagleFeatherAsync();
        var response = await PostAsync("/api/user-eagle-feathers", user.Token,
            CreateUserEagleFeatherRequestFactory.Make(featherId));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    protected async Task<int> CreatePostAsync(TestUser author, int? organisationId = null)
    {
        var uefId = await CreateUefAsync(author);
        var response = await PostAsync($"/api/user-eagle-feathers/{uefId}/posts", author.Token,
            new { content = "achievement", imageUrl = (string?)null, organisationId, challengeId = (int?)null });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    protected async Task<int> CreateCommentAsync(TestUser author, string url)
    {
        var response = await PostAsync(url, author.Token, CreateCommentRequestFactory.Make());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    protected async Task ApproveUefAsync(int uefId, TestUser leader)
    {
        var response = await PostAsync($"/api/user-eagle-feathers/{uefId}/approve", leader.Token, new { note = (string?)null });
        response.EnsureSuccessStatusCode();
    }

    protected static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    protected static async Task<List<int>> ItemIdsAsync(HttpResponseMessage response)
    {
        var body = await JsonAsync(response);
        return body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
    }
}
