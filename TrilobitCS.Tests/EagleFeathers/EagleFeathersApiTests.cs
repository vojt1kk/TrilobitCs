using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.EagleFeathers;

[Collection("Api")]
public class EagleFeathersApiTests
{
    private readonly TrilobitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EagleFeathersApiTests(TrilobitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================
    // GET /api/eagle-feathers
    // =====================

    [Fact]
    public async Task GetEagleFeathers_Authenticated_Returns200WithArray()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetEagleFeathers_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/eagle-feathers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // GET /api/eagle-feathers/{id}
    // =====================

    [Fact]
    public async Task GetEagleFeather_ById_Returns200WithCorrectShape()
    {
        var featherId = await SeedEagleFeather();
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/eagle-feathers/{featherId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(featherId);
        body.GetProperty("light").GetInt32().Should().Be(1);
        body.GetProperty("section").GetString().Should().Be("1A");
        body.GetProperty("number").GetInt32().Should().Be(1);
        body.GetProperty("name").GetString().Should().Be("Test Feather");
        body.GetProperty("challenge").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("grandChallenge").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("sourceUrl").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEagleFeather_ById_NotFound_Returns404()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEagleFeather_ById_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/eagle-feathers/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // Helpers
    // =====================

    private async Task<string> RegisterAndGetToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<int> SeedEagleFeather()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var feather = new EagleFeather
        {
            Light = 1,
            Section = "1A",
            Number = 1,
            Name = "Test Feather",
            Challenge = "Complete a test task",
            GrandChallenge = "Complete a grand test task",
            SourceUrl = "https://example.com/feather",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.EagleFeathers.Add(feather);
        await db.SaveChangesAsync();
        return feather.Id;
    }
}
