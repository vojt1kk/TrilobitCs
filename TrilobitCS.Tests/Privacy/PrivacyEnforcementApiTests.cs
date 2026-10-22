using System.Net;
using FluentAssertions;
using TrilobitCS.Tests.Infrastructure;
using Xunit;

namespace TrilobitCS.Tests.Privacy;

// R1 guard: every endpoint exposing user content must have a row here.
// Viewer roles against a PRIVATE target user T who belongs to organisation O.
[Collection("Api")]
public class PrivacyEnforcementApiTests : SocialApiTestBase
{
    public PrivacyEnforcementApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    public enum Viewer { Stranger, Follower, OrgMate, Self }

    private sealed record Scenario(
        TestUser Target, TestUser Stranger, TestUser Follower, TestUser OrgMate, TestUser PublicOrgAuthor,
        int OrgId, int PersonalPost, int OrgPost, int PublicOrgPost,
        int PersonalComment, int PersonalReply, int OrgComment, int OrgReply)
    {
        public TestUser For(Viewer v) => v switch
        {
            Viewer.Stranger => Stranger,
            Viewer.Follower => Follower,
            Viewer.OrgMate => OrgMate,
            _ => Target,
        };
    }

    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static Scenario? _scenario;

    // Built once (bcrypt registrations are slow). Every (viewer, endpoint) pair is requested at most once,
    // so mutating cases (like / comment) never collide with themselves.
    private async Task<Scenario> GetScenarioAsync()
    {
        await BuildLock.WaitAsync();
        try
        {
            if (_scenario is not null) return _scenario;

            var (leader, orgId) = await CreateLeaderWithOrganisationAsync();
            var target = await CreateUserAsync(isPrivate: true, organisationId: orgId);
            var stranger = await CreateUserAsync();
            var follower = await CreateUserAsync();
            var orgMate = await CreateUserAsync(organisationId: orgId);
            var publicAuthor = await CreateUserAsync(organisationId: orgId);
            await FollowAsync(follower, target);

            var personalPost = await CreatePostAsync(target);
            var orgPost = await CreatePostAsync(target, orgId);
            var publicOrgPost = await CreatePostAsync(publicAuthor, orgId);

            var personalComment = await CreateCommentAsync(target, $"/api/posts/{personalPost}/comments");
            var personalReply = await CreateCommentAsync(target, $"/api/comments/{personalComment}/comments");
            var orgComment = await CreateCommentAsync(target, $"/api/posts/{orgPost}/comments");
            var orgReply = await CreateCommentAsync(target, $"/api/comments/{orgComment}/comments");

            _scenario = new Scenario(target, stranger, follower, orgMate, publicAuthor, orgId,
                personalPost, orgPost, publicOrgPost, personalComment, personalReply, orgComment, orgReply);
            return _scenario;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    private sealed record Endpoint(
        HttpMethod Method, Func<Scenario, string> Url, Func<Viewer, bool> Allowed,
        HttpStatusCode DeniedStatus, string DeniedKey, bool WithBody = false);

    private static readonly object CommentBody = new { content = "hello" };

    private static bool FollowerOrSelf(Viewer v) => v is Viewer.Follower or Viewer.Self;
    private static bool FollowerOrgMateOrSelf(Viewer v) => v != Viewer.Stranger;

    private static Endpoint Profile(string suffix) => new(HttpMethod.Get, s => $"/api/users/{s.Target.Id}/{suffix}",
        FollowerOrSelf, HttpStatusCode.Forbidden, "errors.user_is_private");

    private static Endpoint PostGate(HttpMethod m, Func<Scenario, string> url, Func<Viewer, bool> allowed, string key, bool body = false)
        => new(m, url, allowed, HttpStatusCode.NotFound, key, body);

    private const string PostNotFound = "errors.post_not_found";
    private const string LikeNotFound = "errors.likeable_target_not_found";
    private const string CommentNotFound = "errors.commentable_target_not_found";

    private static readonly Dictionary<string, Endpoint> Endpoints = new()
    {
        // Profile gate (403)
        ["profile-posts"] = Profile("posts"),
        ["profile-eagle-feathers"] = Profile("eagle-feathers"),
        ["profile-stats"] = Profile("stats"),
        ["profile-followers"] = Profile("followers"),
        ["profile-following"] = Profile("following"),

        // Personal post (no org): visible to follower and self only
        ["personal-get-post"] = PostGate(HttpMethod.Get, s => $"/api/posts/{s.PersonalPost}", FollowerOrSelf, PostNotFound),
        ["personal-like-post"] = PostGate(HttpMethod.Post, s => $"/api/posts/{s.PersonalPost}/likes", FollowerOrSelf, LikeNotFound),
        ["personal-list-comments"] = PostGate(HttpMethod.Get, s => $"/api/posts/{s.PersonalPost}/comments", FollowerOrSelf, CommentNotFound),
        ["personal-create-comment"] = PostGate(HttpMethod.Post, s => $"/api/posts/{s.PersonalPost}/comments", FollowerOrSelf, CommentNotFound, true),
        ["personal-list-replies"] = PostGate(HttpMethod.Get, s => $"/api/comments/{s.PersonalComment}/comments", FollowerOrSelf, CommentNotFound),
        ["personal-create-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.PersonalComment}/comments", FollowerOrSelf, CommentNotFound, true),
        ["personal-like-comment"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.PersonalComment}/likes", FollowerOrSelf, LikeNotFound),
        ["personal-list-nested-replies"] = PostGate(HttpMethod.Get, s => $"/api/comments/{s.PersonalReply}/comments", FollowerOrSelf, CommentNotFound),
        ["personal-create-nested-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.PersonalReply}/comments", FollowerOrSelf, CommentNotFound, true),
        ["personal-like-nested-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.PersonalReply}/likes", FollowerOrSelf, LikeNotFound),

        // Org post of a private author: additionally visible to org-mates
        ["org-get-post"] = PostGate(HttpMethod.Get, s => $"/api/posts/{s.OrgPost}", FollowerOrgMateOrSelf, PostNotFound),
        ["org-like-post"] = PostGate(HttpMethod.Post, s => $"/api/posts/{s.OrgPost}/likes", FollowerOrgMateOrSelf, LikeNotFound),
        ["org-list-comments"] = PostGate(HttpMethod.Get, s => $"/api/posts/{s.OrgPost}/comments", FollowerOrgMateOrSelf, CommentNotFound),
        ["org-create-comment"] = PostGate(HttpMethod.Post, s => $"/api/posts/{s.OrgPost}/comments", FollowerOrgMateOrSelf, CommentNotFound, true),
        ["org-list-replies"] = PostGate(HttpMethod.Get, s => $"/api/comments/{s.OrgComment}/comments", FollowerOrgMateOrSelf, CommentNotFound),
        ["org-create-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.OrgComment}/comments", FollowerOrgMateOrSelf, CommentNotFound, true),
        ["org-like-comment"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.OrgComment}/likes", FollowerOrgMateOrSelf, LikeNotFound),
        ["org-list-nested-replies"] = PostGate(HttpMethod.Get, s => $"/api/comments/{s.OrgReply}/comments", FollowerOrgMateOrSelf, CommentNotFound),
        ["org-create-nested-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.OrgReply}/comments", FollowerOrgMateOrSelf, CommentNotFound, true),
        ["org-like-nested-reply"] = PostGate(HttpMethod.Post, s => $"/api/comments/{s.OrgReply}/likes", FollowerOrgMateOrSelf, LikeNotFound),
    };

    public static IEnumerable<object[]> Matrix()
    {
        foreach (var name in Endpoints.Keys)
            foreach (var viewer in Enum.GetValues<Viewer>())
                yield return [name, viewer];
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task GatedEndpoint_ReturnsExpectedResult_ForViewerRole(string endpointName, Viewer viewer)
    {
        var scenario = await GetScenarioAsync();
        var endpoint = Endpoints[endpointName];

        var response = await SendAsync(endpoint.Method, endpoint.Url(scenario), scenario.For(viewer).Token,
            endpoint.WithBody ? CommentBody : null);

        if (endpoint.Allowed(viewer))
        {
            response.IsSuccessStatusCode.Should().BeTrue($"{viewer} may use {endpointName}, got {(int)response.StatusCode}");
        }
        else
        {
            response.StatusCode.Should().Be(endpoint.DeniedStatus);
            (await JsonAsync(response)).GetProperty("message").GetString().Should().Be(endpoint.DeniedKey);
        }
    }

    // Unlike is intentionally ungated: 204 for everyone (a gate would make 404 vs 204 distinguishable).
    [Theory]
    [InlineData(Viewer.Stranger)]
    [InlineData(Viewer.Follower)]
    [InlineData(Viewer.OrgMate)]
    [InlineData(Viewer.Self)]
    public async Task UnlikePost_Returns204_ForEveryViewer(Viewer viewer)
    {
        var scenario = await GetScenarioAsync();

        var response = await SendAsync(HttpMethod.Delete, $"/api/posts/{scenario.PersonalPost}/likes", scenario.For(viewer).Token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(Viewer.Stranger)]
    [InlineData(Viewer.OrgMate)]
    public async Task UnlikeComment_Returns204_ForViewersWithoutAccess(Viewer viewer)
    {
        var scenario = await GetScenarioAsync();

        var response = await SendAsync(HttpMethod.Delete, $"/api/comments/{scenario.PersonalComment}/likes", scenario.For(viewer).Token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =====================
    // Hidden vs non-existent must be indistinguishable
    // =====================

    private const int MissingId = 999999999;

    [Theory]
    [InlineData("GET", "/api/posts/{0}")]
    [InlineData("POST", "/api/posts/{0}/likes")]
    [InlineData("GET", "/api/posts/{0}/comments")]
    [InlineData("POST", "/api/posts/{0}/comments")]
    public async Task HiddenPost_ResponseIsIdenticalToNonExistentPost(string method, string template)
    {
        var scenario = await GetScenarioAsync();
        var httpMethod = new HttpMethod(method);
        object? body = template.EndsWith("/comments") && method == "POST" ? CommentBody : null;

        var hidden = await SendAsync(httpMethod, string.Format(template, scenario.PersonalPost), scenario.Stranger.Token, body);
        var missing = await SendAsync(httpMethod, string.Format(template, MissingId), scenario.Stranger.Token, body);

        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        hidden.StatusCode.Should().Be(missing.StatusCode);
        (await hidden.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("GET", "/api/comments/{0}/comments")]
    [InlineData("POST", "/api/comments/{0}/comments")]
    [InlineData("POST", "/api/comments/{0}/likes")]
    public async Task HiddenComment_ResponseIsIdenticalToNonExistentComment(string method, string template)
    {
        var scenario = await GetScenarioAsync();
        var httpMethod = new HttpMethod(method);
        object? body = template.EndsWith("/comments") && method == "POST" ? CommentBody : null;

        var hidden = await SendAsync(httpMethod, string.Format(template, scenario.PersonalReply), scenario.Stranger.Token, body);
        var missing = await SendAsync(httpMethod, string.Format(template, MissingId), scenario.Stranger.Token, body);

        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        hidden.StatusCode.Should().Be(missing.StatusCode);
        (await hidden.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    private static readonly object PostUpdateBody = new { content = "edited", imageUrl = (string?)null };

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteOnHiddenPost_ResponseIsIdenticalToNonExistentPost(string method)
    {
        var scenario = await GetScenarioAsync();
        var httpMethod = new HttpMethod(method);
        object? body = method == "PUT" ? PostUpdateBody : null;

        var hidden = await SendAsync(httpMethod, $"/api/posts/{scenario.PersonalPost}", scenario.Stranger.Token, body);
        var missing = await SendAsync(httpMethod, $"/api/posts/{MissingId}", scenario.Stranger.Token, body);

        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        hidden.StatusCode.Should().Be(missing.StatusCode);
        (await hidden.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteOnHiddenComment_ResponseIsIdenticalToNonExistentComment(string method)
    {
        var scenario = await GetScenarioAsync();
        var httpMethod = new HttpMethod(method);
        object? body = method == "PUT" ? CommentBody : null;

        var hidden = await SendAsync(httpMethod, $"/api/comments/{scenario.PersonalReply}", scenario.Stranger.Token, body);
        var missing = await SendAsync(httpMethod, $"/api/comments/{MissingId}", scenario.Stranger.Token, body);

        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        hidden.StatusCode.Should().Be(missing.StatusCode);
        (await hidden.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteOnVisibleForeignPost_Returns403(string method)
    {
        var scenario = await GetScenarioAsync();

        var response = await SendAsync(new HttpMethod(method), $"/api/posts/{scenario.PublicOrgPost}", scenario.Stranger.Token,
            method == "PUT" ? PostUpdateBody : null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.forbidden");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteOnVisibleForeignComment_Returns403(string method)
    {
        var scenario = await GetScenarioAsync();
        var commentId = await CreateCommentAsync(scenario.PublicOrgAuthor, $"/api/posts/{scenario.PublicOrgPost}/comments");

        var response = await SendAsync(new HttpMethod(method), $"/api/comments/{commentId}", scenario.Stranger.Token,
            method == "PUT" ? CommentBody : null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.forbidden");
    }

    // =====================
    // List filters
    // =====================

    [Theory]
    [InlineData(Viewer.Follower, true)]
    [InlineData(Viewer.OrgMate, true)]
    [InlineData(Viewer.Self, true)]
    [InlineData(Viewer.Stranger, false)]
    public async Task OrganisationFeed_ShowsPrivateAuthorsOrgPost_OnlyToThoseWhoMaySeeIt(Viewer viewer, bool expectedVisible)
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync($"/api/organisations/{scenario.OrgId}/feed?pageSize=100", scenario.For(viewer).Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await ItemIdsAsync(response);
        if (expectedVisible) ids.Should().Contain(scenario.OrgPost);
        else ids.Should().NotContain(scenario.OrgPost);
    }

    [Fact]
    public async Task OrganisationFeed_ShowsPublicAuthorsOrgPost_ToStranger()
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync($"/api/organisations/{scenario.OrgId}/feed?pageSize=100", scenario.Stranger.Token);

        (await ItemIdsAsync(response)).Should().Contain(scenario.PublicOrgPost);
    }

    [Fact]
    public async Task OrganisationFeed_NeverContainsPostsWithoutOrganisation()
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync($"/api/organisations/{scenario.OrgId}/feed?pageSize=100", scenario.Follower.Token);

        (await ItemIdsAsync(response)).Should().NotContain(scenario.PersonalPost);
    }

    [Theory]
    [InlineData(Viewer.Follower)]
    [InlineData(Viewer.Self)]
    public async Task UserPosts_ListsAllPostsOfPrivateUser_ForFollowerAndSelf(Viewer viewer)
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync($"/api/users/{scenario.Target.Id}/posts?pageSize=100", scenario.For(viewer).Token);

        (await ItemIdsAsync(response)).Should().Contain([scenario.PersonalPost, scenario.OrgPost]);
    }

    // D9 regression: personal feed never leaks, regardless of org membership
    [Theory]
    [InlineData(Viewer.Stranger)]
    [InlineData(Viewer.OrgMate)]
    public async Task PersonalFeed_DoesNotLeakPrivateAuthorsPosts(Viewer viewer)
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync("/api/feed?pageSize=100", scenario.For(viewer).Token);

        var ids = await ItemIdsAsync(response);
        ids.Should().NotContain([scenario.PersonalPost, scenario.OrgPost]);
    }

    [Fact]
    public async Task PersonalFeed_ContainsPrivateAuthorsPosts_ForFollower()
    {
        var scenario = await GetScenarioAsync();

        var response = await GetAsync("/api/feed?pageSize=100", scenario.Follower.Token);

        (await ItemIdsAsync(response)).Should().Contain([scenario.PersonalPost, scenario.OrgPost]);
    }

    [Fact]
    public async Task Unfollowing_RevokesAccessToPrivateProfile()
    {
        var scenario = await GetScenarioAsync();
        var temp = await CreateUserAsync();
        await FollowAsync(temp, scenario.Target);
        (await GetAsync($"/api/users/{scenario.Target.Id}/stats", temp.Token)).StatusCode.Should().Be(HttpStatusCode.OK);

        await SendAsync(HttpMethod.Delete, $"/api/users/{scenario.Target.Id}/follow", temp.Token);

        (await GetAsync($"/api/users/{scenario.Target.Id}/stats", temp.Token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =====================
    // D8: publishing into a foreign organisation
    // =====================

    [Fact]
    public async Task CreatePost_Returns403_WhenPublishingIntoOrganisationOfWhichAuthorIsNotMember()
    {
        var (_, foreignOrgId) = await CreateLeaderWithOrganisationAsync();
        var author = await CreateUserAsync();
        var uefId = await CreateUefAsync(author);

        var response = await PostAsync($"/api/user-eagle-feathers/{uefId}/posts", author.Token,
            new { content = "x", imageUrl = (string?)null, organisationId = foreignOrgId, challengeId = (int?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.not_organisation_member");
    }

    [Fact]
    public async Task CreatePost_Returns403_WhenAuthorOfAnotherOrganisationPublishesIntoForeignOne()
    {
        var (_, ownOrgId) = await CreateLeaderWithOrganisationAsync();
        var (_, foreignOrgId) = await CreateLeaderWithOrganisationAsync();
        var author = await CreateUserAsync(organisationId: ownOrgId);
        var uefId = await CreateUefAsync(author);

        var response = await PostAsync($"/api/user-eagle-feathers/{uefId}/posts", author.Token,
            new { content = "x", imageUrl = (string?)null, organisationId = foreignOrgId, challengeId = (int?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePost_Succeeds_WhenPublishingIntoOwnOrganisation()
    {
        var (_, orgId) = await CreateLeaderWithOrganisationAsync();
        var author = await CreateUserAsync(organisationId: orgId);

        var postId = await CreatePostAsync(author, orgId);

        postId.Should().BeGreaterThan(0);
    }
}
