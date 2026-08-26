using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Users;

[Collection("Api")]
public class UsersApiTests : ApiTestBase
{
    public UsersApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // GET /api/users/{id}

    [Fact]
    public async Task GetUser_Authenticated_Returns200WithPublicProfile()
    {
        var accessToken = await RegisterAndGetToken();

        var secondUser = RegisterRequestFactory.Make();
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", secondUser);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondUserId = ExtractUserIdFromJwt(
            (await secondResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("accessToken").GetString()!);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync($"/api/users/{secondUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nickname").GetString().Should().Be(secondUser.Nickname);
        body.TryGetProperty("email", out _).Should().BeFalse("veřejný profil nesmí obsahovat email");
        body.TryGetProperty("password", out _).Should().BeFalse("response nesmí obsahovat hash hesla");
    }

    [Fact]
    public async Task GetCurrentUser_Authenticated_ReturnsProfileWithEmail()
    {
        var registerRequest = RegisterRequestFactory.Make();
        var accessToken = await RegisterAndGetToken(registerRequest);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nickname").GetString().Should().Be(registerRequest.Nickname);
        body.GetProperty("email").GetString().Should().Be(registerRequest.Email);
        body.TryGetProperty("password", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    [Fact]
    public async Task DeleteUser_WithFollowersBothWays_Returns204()
    {
        // Regression test: Follower.FollowingUser FK is Restrict, so deleting a user who has
        // followers would previously 500 without the Followers cleanup in DeleteUserHandler.
        var userA = RegisterRequestFactory.Make();
        var tokenA = await RegisterAndGetToken(userA);
        var userIdA = ExtractUserIdFromJwt(tokenA);

        var userB = RegisterRequestFactory.Make();
        var tokenB = await RegisterAndGetToken(userB);
        var userIdB = ExtractUserIdFromJwt(tokenB);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // B follows A (A is followed) and A follows B (A is a follower).
            db.Followers.Add(new Follower { FollowerId = userIdB, FollowingId = userIdA, CreatedAt = DateTime.UtcNow });
            db.Followers.Add(new Follower { FollowerId = userIdA, FollowingId = userIdB, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var response = await _client.DeleteAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Users.AnyAsync(u => u.Id == userIdA)).Should().BeFalse();
        (await verifyDb.Followers.AnyAsync(f => f.FollowerId == userIdA || f.FollowingId == userIdA)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_WithPostsLikedAndCommentedByOthers_Returns204NoOrphans()
    {
        var owner = RegisterRequestFactory.Make();
        var ownerToken = await RegisterAndGetToken(owner);
        var ownerId = ExtractUserIdFromJwt(ownerToken);

        var otherToken = await RegisterAndGetToken();
        var otherId = ExtractUserIdFromJwt(otherToken);

        int postId;
        int commentId;
        int replyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            postId = await SeedPostForUserAsync(db, ownerId);

            var comment = new Comment { UserId = otherId, CommentableType = CommentableType.Posts, CommentableId = postId, Content = "nice job", CreatedAt = DateTime.UtcNow };
            db.Comments.Add(comment);
            await db.SaveChangesAsync();

            var reply = new Comment { UserId = otherId, CommentableType = CommentableType.Comments, CommentableId = comment.Id, Content = "agreed", CreatedAt = DateTime.UtcNow };
            db.Comments.Add(reply);
            db.Likes.Add(new Like { UserId = otherId, LikeableType = LikeableType.Posts, LikeableId = postId, CreatedAt = DateTime.UtcNow });
            db.Likes.Add(new Like { UserId = otherId, LikeableType = LikeableType.Comments, LikeableId = comment.Id, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            commentId = comment.Id;
            replyId = reply.Id;
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.DeleteAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Users.AnyAsync(u => u.Id == ownerId)).Should().BeFalse();
        (await verifyDb.Posts.AnyAsync(p => p.Id == postId)).Should().BeFalse();
        (await verifyDb.Comments.AnyAsync(c => c.CommentableType == CommentableType.Posts && c.CommentableId == postId)).Should().BeFalse();
        (await verifyDb.Comments.AnyAsync(c => c.Id == replyId)).Should().BeFalse();
        (await verifyDb.Likes.AnyAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().BeFalse();
        (await verifyDb.Likes.AnyAsync(l => l.LikeableType == LikeableType.Comments && l.LikeableId == commentId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_WithOwnCommentLikedByOther_Returns204NoOrphanedLike()
    {
        var owner = RegisterRequestFactory.Make();
        var ownerToken = await RegisterAndGetToken(owner);
        var ownerId = ExtractUserIdFromJwt(ownerToken);

        var otherToken = await RegisterAndGetToken();
        var otherId = ExtractUserIdFromJwt(otherToken);

        int ownCommentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var postId = await SeedPostForUserAsync(db, ownerId);

            var ownComment = new Comment { UserId = ownerId, CommentableType = CommentableType.Posts, CommentableId = postId, Content = "my own comment", CreatedAt = DateTime.UtcNow };
            db.Comments.Add(ownComment);
            await db.SaveChangesAsync();

            db.Likes.Add(new Like { UserId = otherId, LikeableType = LikeableType.Comments, LikeableId = ownComment.Id, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            ownCommentId = ownComment.Id;
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.DeleteAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Comments.AnyAsync(c => c.Id == ownCommentId)).Should().BeFalse();
        (await verifyDb.Likes.AnyAsync(l => l.LikeableType == LikeableType.Comments && l.LikeableId == ownCommentId)).Should().BeFalse();
    }

    // Seeds an EagleFeather + UserEagleFeather + Post directly via the DbContext for the given
    // user, so DeleteUser cascade tests don't need to go through the full UEF/Post HTTP flow.
    private static async Task<int> SeedPostForUserAsync(AppDbContext db, int userId)
    {
        var feather = new EagleFeather
        {
            Light = 1,
            Section = $"T{Guid.NewGuid():N}"[..10],
            Number = 1,
            Name = "Test pero",
            Challenge = "cin",
            GrandChallenge = "velky cin",
            SourceUrl = "https://example.com",
        };
        db.EagleFeathers.Add(feather);
        await db.SaveChangesAsync();

        var uef = new UserEagleFeather { UserId = userId, EagleFeatherId = feather.Id, CreatedAt = DateTime.UtcNow };
        db.UserEagleFeathers.Add(uef);
        await db.SaveChangesAsync();

        var post = new Post { UserId = userId, UserEagleFeatherId = uef.Id, Content = "achievement", CreatedAt = DateTime.UtcNow };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        return post.Id;
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
