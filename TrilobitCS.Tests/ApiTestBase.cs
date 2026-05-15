using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Tests.Factories;

namespace TrilobitCS.Tests;

public abstract class ApiTestBase
{
    protected readonly TrilobitWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    protected ApiTestBase(TrilobitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected async Task<string> RegisterAndGetToken(RegisterRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request ?? RegisterRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    protected async Task<string> RegisterLeaderAndGetToken(RegisterRequest? request = null)
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

    protected async Task<(string AccessToken, int OrgId)> CreateOrganisationAsLeader()
    {
        var accessToken = await RegisterLeaderAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var orgResponse = await _client.PostAsJsonAsync("/api/organisations", CreateOrganisationRequestFactory.Make());
        var orgBody = await orgResponse.Content.ReadFromJsonAsync<JsonElement>();
        return (accessToken, orgBody.GetProperty("id").GetInt32());
    }
}
