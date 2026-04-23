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

namespace TrilobitCS.Tests.Users;

[Collection("Api")]
public class UsersApiTests
{
    private readonly TrilobitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersApiTests(TrilobitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // GET /api/users/{id}

    [Fact]
    public async Task GetUser_Authenticated_Returns200WithProfile()
    {
        var accessToken = await RegisterAndGetToken();

        var secondUser = RegisterRequestFactory.Make();
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", secondUser);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondUserId = ExtractUserIdFromJwt(
            (await secondResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("accessToken").GetString()!);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync($"/api/users/{secondUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nickname").GetString().Should().Be(secondUser.Nickname);
        body.GetProperty("email").GetString().Should().Be(secondUser.Email);
        body.TryGetProperty("password", out _).Should().BeFalse("response nesmí obsahovat hash hesla");
    }

    [Fact]
    public async Task GetUser_NotFound_Returns404()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/users/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.user_not_found");
    }

    [Fact]
    public async Task GetUser_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // PUT /api/user

    [Fact]
    public async Task UpdateUser_ValidRequest_Returns200AndUpdates()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var update = UpdateUserRequestFactory.Make();
        var response = await _client.PutAsJsonAsync("/api/user", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nickname").GetString().Should().Be(update.Nickname);
        body.GetProperty("firstName").GetString().Should().Be(update.FirstName);
        body.GetProperty("lastName").GetString().Should().Be(update.LastName);
    }

    [Fact]
    public async Task UpdateUser_SameNickname_Returns200()
    {
        var registerRequest = RegisterRequestFactory.Make();
        var accessToken = await RegisterAndGetToken(registerRequest);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var update = UpdateUserRequestFactory.Make() with { Nickname = registerRequest.Nickname };
        var response = await _client.PutAsJsonAsync("/api/user", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_DuplicateNickname_Returns422()
    {
        var firstUser = RegisterRequestFactory.Make();
        await _client.PostAsJsonAsync("/api/auth/register", firstUser);

        var secondUser = RegisterRequestFactory.Make();
        var accessToken = await RegisterAndGetToken(secondUser);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var update = UpdateUserRequestFactory.Make() with { Nickname = firstUser.Nickname };
        var response = await _client.PutAsJsonAsync("/api/user", update);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("errors.nickname_taken");
    }

    [Fact]
    public async Task UpdateUser_InvalidData_Returns422()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var update = UpdateUserRequestFactory.Make() with { Nickname = "" };
        var response = await _client.PutAsJsonAsync("/api/user", update);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUser_FutureBirthDate_Returns422()
    {
        var accessToken = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var update = UpdateUserRequestFactory.Make() with
        {
            BirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
        };
        var response = await _client.PutAsJsonAsync("/api/user", update);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUser_Unauthenticated_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/user", UpdateUserRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // DELETE /api/user

    [Fact]
    public async Task DeleteUser_Authenticated_Returns204AndWipesUserAndTokens()
    {
        var registerRequest = RegisterRequestFactory.Make();
        var accessToken = await RegisterAndGetToken(registerRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.DeleteAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Users.AnyAsync(u => u.Email == registerRequest.Email)).Should().BeFalse();
        (await db.RefreshTokens.CountAsync(t => t.User.Email == registerRequest.Email)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteUser_Unauthenticated_Returns401()
    {
        var response = await _client.DeleteAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> RegisterAndGetToken(RegisterRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request ?? RegisterRequestFactory.Make());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private static int ExtractUserIdFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
            .Replace('-', '+').Replace('_', '/');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        return int.Parse(JsonDocument.Parse(json).RootElement.GetProperty("sub").GetString()!);
    }
}
