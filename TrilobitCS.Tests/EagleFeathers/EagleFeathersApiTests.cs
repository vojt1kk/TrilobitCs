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
public class EagleFeathersApiTests : ApiTestBase
{
    public EagleFeathersApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // =====================
    // GET /api/eagle-feathers
    // =====================

    [Fact]
    public async Task GetEagleFeathers_Authenticated_Returns200WithPagedResponse()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(20);
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("totalPages").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetEagleFeathers_WithPageSize_LimitsResults()
    {
        await SeedEagleFeather();
        await SeedEagleFeather(section: "1B", number: 2);
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetEagleFeathers_PageBeyondRange_Returns200WithEmptyItems()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers?page=9999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetEagleFeathers_InvalidPageSize_Returns422()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers?pageSize=200");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetEagleFeathers_InvalidPage_Returns422()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/eagle-feathers?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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
        body.GetProperty("section").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("number").GetInt32().Should().BeGreaterThan(0);
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

    private static int _featherSeed = 0;

    private async Task<int> SeedEagleFeather(string? section = null, short number = 0)
    {
        var unique = (short)(Interlocked.Increment(ref _featherSeed) % 900 + 100);
        section ??= $"T{unique}";
        if (number == 0) number = unique;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var feather = new EagleFeather
        {
            Light = 1,
            Section = section,
            Number = number,
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
