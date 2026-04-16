using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Auth;

[Collection("Api")]
public class LogoutApiTests
{
    private readonly HttpClient _client;

    public LogoutApiTests(TrilobitWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_ValidToken_Returns204()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequestFactory.Make());
        var body = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = body.GetProperty("refreshToken").GetString()!;

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidToken_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = "neexistujici-token",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
