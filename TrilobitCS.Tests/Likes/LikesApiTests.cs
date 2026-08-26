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

namespace TrilobitCS.Tests.Likes;

[Collection("Api")]
public class LikesApiTests : ApiTestBase
{
    public LikesApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // Shared container across tests → eagle feathers must be unique on (Light, Section, Number).
    private static int _featherSeq;

    // =====================
    // POST /api/posts/{id}/likes
    // =====================

    [Fact]
    public async Task LikePost_CreatesRow_Returns200()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();

        var response = await _client.PostAsync($"/api/posts/{postId}/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("likeableId").GetInt32().Should().Be(postId);
        body.GetProperty("likeableType").GetInt32().Should().Be((int)LikeableType.Posts);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().Be(1);
    }

    [Fact]
    public async Task LikeComment_CreatesRow_Returns200()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();
        var commentId = await CreateCommentAndGetId(postId);

        var response = await _client.PostAsync($"/api/comments/{commentId}/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("likeableId").GetInt32().Should().Be(commentId);
        body.GetProperty("likeableType").GetInt32().Should().Be((int)LikeableType.Comments);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Comments && l.LikeableId == commentId)).Should().Be(1);
    }

    [Fact]
    public async Task LikePost_Repeat_IsIdempotent_ReturnsSameRow()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();

        var first = await _client.PostAsync($"/api/posts/{postId}/likes", null);
        var second = await _client.PostAsync($"/api/posts/{postId}/likes", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
        secondBody.GetProperty("id").GetInt32().Should().Be(firstBody.GetProperty("id").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().Be(1);
    }

    [Fact]
    public async Task LikePost_Repeat_Concurrent_BothReturn200AndExactlyOneRowExists()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();

        var task1 = _client.PostAsync($"/api/posts/{postId}/likes", null);
        var task2 = _client.PostAsync($"/api/posts/{postId}/likes", null);
        var responses = await Task.WhenAll(task1, task2);

        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().Be(1);
    }

    [Fact]
    public async Task LikePost_NonexistentPost_Returns404()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsync("/api/posts/999999/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LikeComment_NonexistentComment_Returns404()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);

        var response = await _client.PostAsync("/api/comments/999999/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LikePost_SelfLike_IsAllowed()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId(); // author == liker

        var response = await _client.PostAsync($"/api/posts/{postId}/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LikePost_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/posts/1/likes", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================
    // DELETE /api/posts/{id}/likes, /api/comments/{id}/likes
    // =====================

    [Fact]
    public async Task UnlikePost_Returns204AndRemovesRow()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();
        await _client.PostAsync($"/api/posts/{postId}/likes", null);

        var response = await _client.DeleteAsync($"/api/posts/{postId}/likes");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().Be(0);
    }

    [Fact]
    public async Task UnlikeComment_Returns204AndRemovesRow()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();
        var commentId = await CreateCommentAndGetId(postId);
        await _client.PostAsync($"/api/comments/{commentId}/likes", null);

        var response = await _client.DeleteAsync($"/api/comments/{commentId}/likes");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Comments && l.LikeableId == commentId)).Should().Be(0);
    }

    [Fact]
    public async Task UnlikePost_NotLiked_Returns204NoOp()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();

        var response = await _client.DeleteAsync($"/api/posts/{postId}/likes");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlikePost_Concurrent_BothReturn204AndNoRowRemains()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();
        await _client.PostAsync($"/api/posts/{postId}/likes", null);

        var task1 = _client.DeleteAsync($"/api/posts/{postId}/likes");
        var task2 = _client.DeleteAsync($"/api/posts/{postId}/likes");
        var responses = await Task.WhenAll(task1, task2);

        responses[0].StatusCode.Should().Be(HttpStatusCode.NoContent);
        responses[1].StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Likes.CountAsync(l => l.LikeableType == LikeableType.Posts && l.LikeableId == postId)).Should().Be(0);
    }

    [Fact]
    public async Task LikePost_ThenUnlike_PostResponseLikeCountReflectsRealCount()
    {
        var token = await RegisterAndGetToken();
        SetAuth(token);
        var postId = await CreatePostAndGetId();

        await _client.PostAsync($"/api/posts/{postId}/likes", null);
        var afterLike = await _client.GetAsync($"/api/posts/{postId}");
        (await afterLike.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("likeCount").GetInt32().Should().Be(1);

        await _client.DeleteAsync($"/api/posts/{postId}/likes");
        var afterUnlike = await _client.GetAsync($"/api/posts/{postId}");
        (await afterUnlike.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("likeCount").GetInt32().Should().Be(0);
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
            Section = $"LK{seq}",
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

    private async Task<int> CreatePostAndGetId()
    {
        var featherId = await SeedEagleFeatherAsync();
        var uefResponse = await _client.PostAsJsonAsync("/api/user-eagle-feathers",
            CreateUserEagleFeatherRequestFactory.Make(featherId));
        var uefBody = await uefResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uefId = uefBody.GetProperty("id").GetInt32();

        var postResponse = await _client.PostAsJsonAsync($"/api/user-eagle-feathers/{uefId}/posts",
            new { content = "achievement", imageUrl = (string?)null, organisationId = (int?)null, challengeId = (int?)null });
        var postBody = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        return postBody.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateCommentAndGetId(int postId)
    {
        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest("nice work"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}
