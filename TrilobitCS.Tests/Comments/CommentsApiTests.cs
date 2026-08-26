using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Tests.Factories;
using Xunit;

namespace TrilobitCS.Tests.Comments;

[Collection("Api")]
public class CommentsApiTests : ApiTestBase
{
    public CommentsApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Shared container across tests → eagle feathers must be unique on (Light, Section, Number).
    // "CT" prefix keeps this class's sequence from colliding with other test classes' seeded feathers.
    private static int _featherSeq;

    // =====================
    // POST /api/posts/{id}/comments
    // =====================

    [Fact]
    public async Task CreateOnPost_Returns200_WithValidRequest()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments",
            CreateCommentRequestFactory.Make("nice post!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("content").GetString().Should().Be("nice post!");
        body.GetProperty("commentableType").GetInt32().Should().Be((int)CommentableType.Posts);
        body.GetProperty("commentableId").GetInt32().Should().Be(postId);
    }

    [Fact]
    public async Task CreateOnPost_Returns404_WhenPostDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsJsonAsync("/api/posts/999999/comments",
            CreateCommentRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOnPost_Returns422_WhenContentEmpty()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments",
            CreateCommentRequestFactory.Make(""));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // =====================
    // POST /api/comments/{id}/comments (replies, multi-level)
    // =====================

    [Fact]
    public async Task CreateOnComment_Returns200_WithReplyToReplyToReply()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var commentId = await CreateCommentOnPost(postId, "top level");
        var replyId = await CreateReply(commentId, "reply 1");
        var replyReplyId = await CreateReply(replyId, "reply 2");
        var replyReplyReplyId = await CreateReply(replyReplyId, "reply 3");

        replyReplyReplyId.Should().BeGreaterThan(0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leaf = await db.Comments.FindAsync(replyReplyReplyId);
        leaf!.CommentableType.Should().Be(CommentableType.Comments);
        leaf.CommentableId.Should().Be(replyReplyId);
    }

    [Fact]
    public async Task CreateOnComment_Returns404_WhenCommentDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsJsonAsync("/api/comments/999999/comments",
            CreateCommentRequestFactory.Make());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // PUT /api/comments/{id}
    // =====================

    [Fact]
    public async Task Update_Returns200_AsAuthor()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();
        var commentId = await CreateCommentOnPost(postId, "original");

        var response = await _client.PutAsJsonAsync($"/api/comments/{commentId}",
            new { content = "edited" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("content").GetString().Should().Be("edited");
    }

    [Fact]
    public async Task Update_Returns403_WhenNotAuthor()
    {
        var ownerToken = await RegisterAndGetToken();
        SetAuth(ownerToken);
        var postId = await SeedPostAsync();
        var commentId = await CreateCommentOnPost(postId, "original");

        var otherToken = await RegisterAndGetToken();
        SetAuth(otherToken);

        var response = await _client.PutAsJsonAsync($"/api/comments/{commentId}",
            new { content = "hijacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Returns404_WhenCommentDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PutAsJsonAsync("/api/comments/999999",
            new { content = "edited" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // DELETE /api/comments/{id}
    // =====================

    [Fact]
    public async Task Delete_Returns204_AsAuthor()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();
        var commentId = await CreateCommentOnPost(postId, "to delete");

        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Comments.AnyAsync(c => c.Id == commentId)).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Returns403_WhenNotAuthor()
    {
        var ownerToken = await RegisterAndGetToken();
        SetAuth(ownerToken);
        var postId = await SeedPostAsync();
        var commentId = await CreateCommentOnPost(postId, "not yours");

        var otherToken = await RegisterAndGetToken();
        SetAuth(otherToken);

        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns404_WhenCommentDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.DeleteAsync("/api/comments/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_CascadesEntireReplyTree_AndAllLikes()
    {
        var token = await RegisterAndGetToken();
        var userId = await GetUserIdFromToken(token);
        SetAuth(token);
        var postId = await SeedPostAsync();

        // 3-level-deep reply chain: root -> reply1 -> reply2 -> reply3
        var rootId = await CreateCommentOnPost(postId, "root");
        var reply1Id = await CreateReply(rootId, "reply1");
        var reply2Id = await CreateReply(reply1Id, "reply2");
        var reply3Id = await CreateReply(reply2Id, "reply3");

        var allCommentIds = new[] { rootId, reply1Id, reply2Id, reply3Id };

        // Seed a Like on every comment in the chain (no Likes API yet — insert directly).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var id in allCommentIds)
            {
                db.Likes.Add(new Like
                {
                    UserId = userId,
                    LikeableType = LikeableType.Comments,
                    LikeableId = id,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.DeleteAsync($"/api/comments/{rootId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var id in allCommentIds)
        {
            (await verifyDb.Comments.AnyAsync(c => c.Id == id)).Should().BeFalse();
            (await verifyDb.Likes.AnyAsync(l => l.LikeableType == LikeableType.Comments && l.LikeableId == id)).Should().BeFalse();
        }
    }

    // =====================
    // GET /api/posts/{id}/comments, /api/comments/{id}/comments
    // =====================

    [Fact]
    public async Task GetForPost_Returns200_DirectChildrenOnly()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var c1 = await CreateCommentOnPost(postId, "top 1");
        var c2 = await CreateCommentOnPost(postId, "top 2");
        await CreateReply(c1, "a reply, should not appear at top level");

        var response = await _client.GetAsync($"/api/posts/{postId}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(2);
        var ids = body.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("id").GetInt32())
            .ToList();
        ids.Should().BeEquivalentTo(new[] { c1, c2 });
    }

    [Fact]
    public async Task GetForComment_Returns200_DirectRepliesOnly()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await SeedPostAsync();

        var rootId = await CreateCommentOnPost(postId, "root");
        var reply1Id = await CreateReply(rootId, "reply1");
        await CreateReply(reply1Id, "reply-to-reply, should not appear under root");

        var response = await _client.GetAsync($"/api/comments/{rootId}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
        body.GetProperty("items")[0].GetProperty("id").GetInt32().Should().Be(reply1Id);
    }

    [Fact]
    public async Task GetForPost_Returns404_WhenPostDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/posts/999999/comments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetForComment_Returns404_WhenCommentDoesNotExist()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.GetAsync("/api/comments/999999/comments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =====================
    // Helpers
    // =====================

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> SeedEagleFeatherAsync()
    {
        var seq = Interlocked.Increment(ref _featherSeq);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ef = new EagleFeather
        {
            Light = (byte)((seq % 4) + 1),
            Section = $"CT{seq}",
            Number = (short)seq,
            Name = "Test pero",
            Challenge = "cin",
            GrandChallenge = "velky cin",
            SourceUrl = "https://example.com",
        };
        db.EagleFeathers.Add(ef);
        await db.SaveChangesAsync();
        return ef.Id;
    }

    private async Task<int> CreateUefAndGetId(int eagleFeatherId)
    {
        var response = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(eagleFeatherId));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> SeedPostAsync()
    {
        var featherId = await SeedEagleFeatherAsync();
        var uefId = await CreateUefAndGetId(featherId);
        var response = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/posts",
            new { content = "achievement", imageUrl = (string?)null, organisationId = (int?)null, challengeId = (int?)null });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateCommentOnPost(int postId, string content)
    {
        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments",
            CreateCommentRequestFactory.Make(content));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateReply(int commentId, string content)
    {
        var response = await _client.PostAsJsonAsync($"/api/comments/{commentId}/comments",
            CreateCommentRequestFactory.Make(content));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> GetUserIdFromToken(string token)
    {
        var previous = _client.DefaultRequestHeaders.Authorization;
        SetAuth(token);
        var response = await _client.GetAsync("/api/user/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = previous;
        return body.GetProperty("id").GetInt32();
    }
}
