using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Shared;

public static class PrivacyGuard
{
    // User.Followers is mapped via FollowingId, so Followers.Any(f => f.FollowerId == viewerId) means "viewer follows u".
    public static Expression<Func<User, bool>> ProfileVisibleTo(int viewerId)
        => u => u.Id == viewerId || !u.IsPrivate || u.Followers.Any(f => f.FollowerId == viewerId);

    // Written out explicitly (not composed from ProfileVisibleTo) so EF can translate it without expression visitors.
    // The org clause is omitted for viewers without an org to avoid generating "organisation_id = NULL".
    public static Expression<Func<Post, bool>> PostVisibleTo(int viewerId, int? viewerOrgId)
    {
        if (viewerOrgId is null)
            return p => p.UserId == viewerId
                        || !p.User.IsPrivate
                        || p.User.Followers.Any(f => f.FollowerId == viewerId);

        var orgId = viewerOrgId.Value;
        return p => p.UserId == viewerId
                    || !p.User.IsPrivate
                    || p.User.Followers.Any(f => f.FollowerId == viewerId)
                    || p.OrganisationId == orgId;
    }

    public static Task<int?> GetViewerOrganisationIdAsync(AppDbContext db, int viewerId, CancellationToken ct)
        => db.Users.Where(u => u.Id == viewerId).Select(u => u.OrganisationId).FirstOrDefaultAsync(ct);

    public static async Task EnsureProfileVisibleAsync(AppDbContext db, int viewerId, int targetId, CancellationToken ct)
    {
        var target = await db.Users
            .Where(u => u.Id == targetId)
            .Select(u => new { Visible = u.Id == viewerId || !u.IsPrivate || u.Followers.Any(f => f.FollowerId == viewerId) })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("errors.user_not_found");

        if (!target.Visible)
            throw new ForbiddenException("errors.user_is_private");
    }

    // Returns false for both hidden and non-existent posts; callers throw their own "target not found" key
    // so the two cases stay indistinguishable.
    public static async Task<bool> IsPostVisibleAsync(AppDbContext db, int viewerId, int postId, CancellationToken ct)
    {
        var viewerOrgId = await GetViewerOrganisationIdAsync(db, viewerId, ct);
        return await db.Posts
            .Where(p => p.Id == postId)
            .AnyAsync(PostVisibleTo(viewerId, viewerOrgId), ct);
    }

    // Walks up the reply chain to the root post. Returns null for orphaned chains (or cycles).
    public static async Task<int?> ResolveRootPostIdAsync(
        AppDbContext db, CommentableType type, int id, CancellationToken ct)
    {
        var seen = new HashSet<int>();
        while (type == CommentableType.Comments)
        {
            if (!seen.Add(id))
                return null;

            var parent = await db.Comments
                .Where(c => c.Id == id)
                .Select(c => new { c.CommentableType, c.CommentableId })
                .FirstOrDefaultAsync(ct);
            if (parent is null)
                return null;

            type = parent.CommentableType;
            id = parent.CommentableId;
        }

        return id;
    }
}
