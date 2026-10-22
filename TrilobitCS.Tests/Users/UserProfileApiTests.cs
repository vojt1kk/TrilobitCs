using System.Net;
using System.Text.Json;
using FluentAssertions;
using TrilobitCS.Tests.Factories;
using TrilobitCS.Tests.Infrastructure;
using Xunit;

namespace TrilobitCS.Tests.Users;

[Collection("Api")]
public class UserProfileApiTests : SocialApiTestBase
{
    public UserProfileApiTests(TrilobitWebApplicationFactory factory) : base(factory) { }

    // =====================
    // GET /api/users/{id}/posts (C1)
    // =====================

    [Fact]
    public async Task GetUserPosts_ReturnsPostsNewestFirst()
    {
        var author = await CreateUserAsync();
        var viewer = await CreateUserAsync();
        var first = await CreatePostAsync(author);
        var second = await CreatePostAsync(author);

        var response = await GetAsync($"/api/users/{author.Id}/posts", viewer.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ItemIdsAsync(response)).Should().Equal(second, first);
    }

    [Fact]
    public async Task GetUserPosts_ExcludesPostsOfOtherUsers()
    {
        var author = await CreateUserAsync();
        var other = await CreateUserAsync();
        var otherPost = await CreatePostAsync(other);

        var response = await GetAsync($"/api/users/{author.Id}/posts", author.Token);

        (await ItemIdsAsync(response)).Should().NotContain(otherPost);
    }

    [Fact]
    public async Task GetUserPosts_ReturnsPostResponseShape()
    {
        var author = await CreateUserAsync();
        await CreatePostAsync(author);

        var response = await GetAsync($"/api/users/{author.Id}/posts", author.Token);

        var item = (await JsonAsync(response)).GetProperty("items")[0];
        item.GetProperty("author").GetProperty("id").GetInt32().Should().Be(author.Id);
        item.TryGetProperty("likeCount", out _).Should().BeTrue();
        item.TryGetProperty("commentCount", out _).Should().BeTrue();
    }

    // =====================
    // GET /api/users/{id}/eagle-feathers (C2)
    // =====================

    [Fact]
    public async Task GetUserEagleFeathers_ShowsAllStatuses_ToSelf()
    {
        var (leader, _) = await CreateLeaderWithOrganisationAsync();
        var user = await CreateUserAsync();
        var approved = await CreateUefAsync(user);
        await ApproveUefAsync(approved, leader);
        var pending = await CreateUefAsync(user);

        var response = await GetAsync($"/api/users/{user.Id}/eagle-feathers", user.Token);

        (await ItemIdsAsync(response)).Should().Contain([approved, pending]);
    }

    [Fact]
    public async Task GetUserEagleFeathers_ShowsOnlyApproved_ToOtherViewers()
    {
        var (leader, _) = await CreateLeaderWithOrganisationAsync();
        var user = await CreateUserAsync();
        var viewer = await CreateUserAsync();
        var approved = await CreateUefAsync(user);
        await ApproveUefAsync(approved, leader);
        var pending = await CreateUefAsync(user);

        var response = await GetAsync($"/api/users/{user.Id}/eagle-feathers", viewer.Token);

        var ids = await ItemIdsAsync(response);
        ids.Should().Contain(approved);
        ids.Should().NotContain(pending);
    }

    // =====================
    // GET /api/users/{id}/stats (C3)
    // =====================

    [Fact]
    public async Task GetUserStats_ReturnsAllFourCounts()
    {
        var (leader, _) = await CreateLeaderWithOrganisationAsync();
        var user = await CreateUserAsync();
        var follower = await CreateUserAsync();
        var followee = await CreateUserAsync();
        await FollowAsync(follower, user);
        await FollowAsync(user, followee);
        await ApproveUefAsync(await CreateUefAsync(user), leader);
        await CreatePostAsync(user);
        await CreatePostAsync(user);

        var response = await GetAsync($"/api/users/{user.Id}/stats", follower.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("postCount").GetInt32().Should().Be(2);
        body.GetProperty("approvedFeatherCount").GetInt32().Should().Be(1);
        body.GetProperty("followerCount").GetInt32().Should().Be(1);
        body.GetProperty("followingCount").GetInt32().Should().Be(1);
    }

    // =====================
    // C4: 404 before privacy, 403 for private
    // =====================

    [Theory]
    [InlineData("posts")]
    [InlineData("eagle-feathers")]
    [InlineData("stats")]
    public async Task ProfileEndpoints_Return404_ForUnknownUser(string suffix)
    {
        var viewer = await CreateUserAsync();

        var response = await GetAsync($"/api/users/999999999/{suffix}", viewer.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.user_not_found");
    }

    [Theory]
    [InlineData("posts")]
    [InlineData("eagle-feathers")]
    [InlineData("stats")]
    public async Task ProfileEndpoints_Return403_ForPrivateNonFollowedUser(string suffix)
    {
        var target = await CreateUserAsync(isPrivate: true);
        var viewer = await CreateUserAsync();

        var response = await GetAsync($"/api/users/{target.Id}/{suffix}", viewer.Token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await JsonAsync(response)).GetProperty("message").GetString().Should().Be("errors.user_is_private");
    }

    // =====================
    // PUT /api/user (D2)
    // =====================

    [Fact]
    public async Task UpdateUser_SetsIsPrivate_WhenProvided()
    {
        var user = await CreateUserAsync();
        var request = UpdateUserRequestFactory.Make() with { IsPrivate = true };

        var response = await SendAsync(HttpMethod.Put, "/api/user", user.Token, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(response)).GetProperty("isPrivate").GetBoolean().Should().BeTrue();
        var me = await JsonAsync(await GetAsync("/api/user/me", user.Token));
        me.GetProperty("isPrivate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_KeepsIsPrivate_WhenOmitted()
    {
        var user = await CreateUserAsync(isPrivate: true);
        var request = UpdateUserRequestFactory.Make();

        var response = await SendAsync(HttpMethod.Put, "/api/user", user.Token, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(response)).GetProperty("isPrivate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_CanSwitchProfileBackToPublic()
    {
        var user = await CreateUserAsync(isPrivate: true);
        var request = UpdateUserRequestFactory.Make() with { IsPrivate = false };

        var response = await SendAsync(HttpMethod.Put, "/api/user", user.Token, request);

        (await JsonAsync(response)).GetProperty("isPrivate").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUser_ResponseIncludesCounts()
    {
        var user = await CreateUserAsync();
        await CreatePostAsync(user);

        var response = await SendAsync(HttpMethod.Put, "/api/user", user.Token, UpdateUserRequestFactory.Make());

        (await JsonAsync(response)).GetProperty("postCount").GetInt32().Should().Be(1);
    }

    // =====================
    // GET /api/user/me (D3)
    // =====================

    [Fact]
    public async Task GetMe_IncludesIsPrivateAndFourCounts()
    {
        var (leader, _) = await CreateLeaderWithOrganisationAsync();
        var user = await CreateUserAsync(isPrivate: true);
        var follower = await CreateUserAsync();
        var followee = await CreateUserAsync();
        await FollowAsync(follower, user);
        await FollowAsync(user, followee);
        await ApproveUefAsync(await CreateUefAsync(user), leader);
        await CreatePostAsync(user);

        var response = await GetAsync("/api/user/me", user.Token);

        var body = await JsonAsync(response);
        body.GetProperty("isPrivate").GetBoolean().Should().BeTrue();
        body.GetProperty("followerCount").GetInt32().Should().Be(1);
        body.GetProperty("followingCount").GetInt32().Should().Be(1);
        body.GetProperty("approvedFeatherCount").GetInt32().Should().Be(1);
        body.GetProperty("postCount").GetInt32().Should().Be(1);
    }

    // =====================
    // GET /api/users/{id} (E1, E2)
    // =====================

    [Fact]
    public async Task GetUser_ReturnsFullCounts_ForPublicProfile()
    {
        var target = await CreateUserAsync();
        var viewer = await CreateUserAsync();
        await CreatePostAsync(target);

        var body = await JsonAsync(await GetAsync($"/api/users/{target.Id}", viewer.Token));

        body.GetProperty("isPrivate").GetBoolean().Should().BeFalse();
        body.GetProperty("postCount").GetInt32().Should().Be(1);
        body.GetProperty("followerCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetUser_ReturnsNullCounts_ForPrivateProfileOfNonFollower()
    {
        var target = await CreateUserAsync(isPrivate: true);
        var viewer = await CreateUserAsync();

        var response = await GetAsync($"/api/users/{target.Id}", viewer.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("isPrivate").GetBoolean().Should().BeTrue();
        body.GetProperty("isFollowing").GetBoolean().Should().BeFalse();
        foreach (var name in new[] { "followerCount", "followingCount", "approvedFeatherCount", "postCount" })
            body.GetProperty(name).ValueKind.Should().Be(JsonValueKind.Null, name);
    }

    [Fact]
    public async Task GetUser_ReturnsFullCountsAndIsFollowing_ForFollowerOfPrivateProfile()
    {
        var target = await CreateUserAsync(isPrivate: true);
        var viewer = await CreateUserAsync();
        await FollowAsync(viewer, target);

        var body = await JsonAsync(await GetAsync($"/api/users/{target.Id}", viewer.Token));

        body.GetProperty("isFollowing").GetBoolean().Should().BeTrue();
        body.GetProperty("followerCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetUser_ReturnsFullCountsAndIsFollowingFalse_ForSelf()
    {
        var user = await CreateUserAsync(isPrivate: true);

        var body = await JsonAsync(await GetAsync($"/api/users/{user.Id}", user.Token));

        body.GetProperty("isFollowing").GetBoolean().Should().BeFalse();
        body.GetProperty("postCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetUser_DoesNotExposeEmail()
    {
        var target = await CreateUserAsync();

        var body = await JsonAsync(await GetAsync($"/api/users/{target.Id}", target.Token));

        body.TryGetProperty("email", out _).Should().BeFalse();
    }

    // =====================
    // Members isFollowing (E3)
    // =====================

    [Fact]
    public async Task GetMembers_SetsIsFollowingPerMember()
    {
        var (leader, orgId) = await CreateLeaderWithOrganisationAsync();
        var followed = await CreateUserAsync(organisationId: orgId);
        var notFollowed = await CreateUserAsync(organisationId: orgId);
        var viewer = await CreateUserAsync(organisationId: orgId);
        await FollowAsync(viewer, followed);

        var response = await GetAsync($"/api/organisations/{orgId}/members?pageSize=100", viewer.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = (await JsonAsync(response)).GetProperty("items").EnumerateArray()
            .ToDictionary(m => m.GetProperty("id").GetInt32(), m => m.GetProperty("isFollowing").GetBoolean());
        members[followed.Id].Should().BeTrue();
        members[notFollowed.Id].Should().BeFalse();
        members[leader.Id].Should().BeFalse();
        members[viewer.Id].Should().BeFalse();
    }

    // =====================
    // E4: exactly one DB command
    // =====================

    [Theory]
    [InlineData("user")]
    [InlineData("me")]
    public async Task ProfileReads_IssueExactlyOneDbCommand(string which)
    {
        var target = await CreateUserAsync(isPrivate: true);
        var viewer = await CreateUserAsync();
        await FollowAsync(viewer, target);
        var (url, token) = which == "me"
            ? ("/api/user/me", target.Token)
            : ($"/api/users/{target.Id}", viewer.Token);
        await GetAsync(url, token); // warm-up: JIT / first-use noise must not count

        _factory.CommandCounter.Start();
        var response = await GetAsync(url, token);
        var commands = _factory.CommandCounter.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        commands.Should().HaveCount(1, string.Join("\n---\n", commands));
    }
}
