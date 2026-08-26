using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Shared;

public static class CommentCascadeHelper
{
    /// <summary>
    /// Iteratively collects all descendant comment IDs (replies, replies-of-replies, ...) for the given
    /// root comment IDs. Cycle-safe via a HashSet seen-set. Returns only descendants, not the roots themselves.
    /// </summary>
    public static async Task<List<int>> CollectDescendantCommentIdsAsync(
        AppDbContext db,
        IReadOnlyCollection<int> rootCommentIds,
        CancellationToken ct)
    {
        var seen = new HashSet<int>(rootCommentIds);
        var frontier = rootCommentIds.ToList();

        while (frontier.Count > 0)
        {
            var next = await db.Comments
                .Where(c => c.CommentableType == CommentableType.Comments && frontier.Contains(c.CommentableId))
                .Select(c => c.Id)
                .ToListAsync(ct);

            var newIds = next.Where(id => !seen.Contains(id)).ToList();
            if (newIds.Count == 0)
                break;

            foreach (var id in newIds)
                seen.Add(id);

            frontier = newIds;
        }

        seen.ExceptWith(rootCommentIds);
        return seen.ToList();
    }

    /// <summary>
    /// Tracks removal of all Likes on the given comment IDs and the comment rows themselves.
    /// Does not call SaveChangesAsync — caller controls the transaction boundary.
    /// </summary>
    public static async Task CascadeDeleteCommentsAndLikesAsync(
        AppDbContext db,
        IReadOnlyCollection<int> commentIds,
        CancellationToken ct)
    {
        if (commentIds.Count == 0)
            return;

        var likes = await db.Likes
            .Where(l => l.LikeableType == LikeableType.Comments && commentIds.Contains(l.LikeableId))
            .ToListAsync(ct);
        db.Likes.RemoveRange(likes);

        var comments = await db.Comments
            .Where(c => commentIds.Contains(c.Id))
            .ToListAsync(ct);
        db.Comments.RemoveRange(comments);
    }
}
