using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Auth;

[Collection("Api")]
public class RegisterApiTests
{
    private readonly HttpClient _client;

    public RegisterApiTests(TrilobitWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_InvalidRequest_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_PasswordMismatch_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            RegisterRequestFactory.Make() with { PasswordConfirm = "jinheslo123" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns422()
    {
        var original = RegisterRequestFactory.Make();
        await _client.PostAsJsonAsync("/api/auth/register", original);

        var duplicate = RegisterRequestFactory.Make() with { Email = original.Email };
        var response = await _client.PostAsJsonAsync("/api/auth/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.email_taken");
    }

    [Fact]
    public async Task Register_DuplicateNickname_Returns422()
    {
        var original = RegisterRequestFactory.Make();
        await _client.PostAsJsonAsync("/api/auth/register", original);

        var duplicate = RegisterRequestFactory.Make() with { Nickname = original.Nickname };
        var response = await _client.PostAsJsonAsync("/api/auth/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.nickname_taken");
    }
}
