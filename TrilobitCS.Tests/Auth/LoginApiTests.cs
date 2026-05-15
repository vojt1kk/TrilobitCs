using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Auth;

[Collection("Api")]
public class LoginApiTests : ApiTestBase
{
    public LoginApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_ValidCredentials_Returns201()
    {
        var request = RegisterRequestFactory.Make();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            nickname = request.Nickname,
            password = request.Password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_UnknownNickname_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            nickname = "neexistujicichuser",
            password = "tajneheslo123",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var request = RegisterRequestFactory.Make();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            nickname = request.Nickname,
            password = "spatneheslo",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
