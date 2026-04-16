using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Auth;

[Collection("Api")]
public class RefreshApiTests
{
    private readonly HttpClient _client;

    public RefreshApiTests(TrilobitWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200()
    {
        var refreshToken = await RegisterAndGetRefreshToken();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "neplatny-token",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UsedToken_Returns401()
    {
        var refreshToken = await RegisterAndGetRefreshToken();
        await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> RegisterAndGetRefreshToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("refreshToken").GetString()!;
    }
}
