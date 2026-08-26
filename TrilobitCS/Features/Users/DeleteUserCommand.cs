using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Users;

public record DeleteUserCommand(int UserId) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly AppDbContext _db;

    public DeleteUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([command.UserId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        // Delete refresh tokens first — no cascade delete on this FK. Pre-existing, accepted
        // non-atomic step (its own immediate transaction); not addressed by this fix.
        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // Followers cleanup: Follower.FollowingUser FK is Restrict, so leaving these rows in
        // place would make the final SaveChangesAsync below fail with a FK violation for any
        // user who has followers. Tracked RemoveRange (not ExecuteDeleteAsync) so this
        // participates in the single atomic SaveChangesAsync at the end of this method.
        var followerRows = await _db.Followers
            .Where(f => f.FollowingId == user.Id || f.FollowerId == user.Id)
            .ToListAsync(cancellationToken);
        _db.Followers.RemoveRange(followerRows);

        // Orphan prevention for the user's own Posts: Post.User FK is Cascade, so the Post rows
        // themselves are removed by the DB when the user is deleted below — but there is no FK
        // from Like/Comment to Post, so their polymorphic Likes/Comments must be cleaned up
        // explicitly before that cascade fires, or they'd be orphaned.
        var ownPosts = await _db.Posts
            .Where(p => p.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var post in ownPosts)
        {
            var topLevelCommentIds = await _db.Comments
                .Where(c => c.CommentableType == CommentableType.Posts && c.CommentableId == post.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var descendantCommentIds = await CommentCascadeHelper.CollectDescendantCommentIdsAsync(_db, topLevelCommentIds, cancellationToken);
            var allCommentIds = topLevelCommentIds.Concat(descendantCommentIds).ToList();
            await CommentCascadeHelper.CascadeDeleteCommentsAndLikesAsync(_db, allCommentIds, cancellationToken);

            var postLikes = await _db.Likes
                .Where(l => l.LikeableType == LikeableType.Posts && l.LikeableId == post.Id)
                .ToListAsync(cancellationToken);
            _db.Likes.RemoveRange(postLikes);
        }

        // Orphan prevention for the user's own Comments (top-level or reply). Includes the
        // comment's own id (not just descendants) so its own Likes are cleaned up too. The
        // resulting tracked RemoveRange on the comment row is harmless even though Comment.User
        // Cascade would also remove it — EF Core dependency-orders the explicit tracked delete
        // before the parent User delete within the same SaveChangesAsync.
        var ownCommentIds = await _db.Comments
            .Where(c => c.UserId == user.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var commentId in ownCommentIds)
        {
            var descendantIds = await CommentCascadeHelper.CollectDescendantCommentIdsAsync(_db, [commentId], cancellationToken);
            var allIds = descendantIds.Append(commentId).ToList();
            await CommentCascadeHelper.CascadeDeleteCommentsAndLikesAsync(_db, allIds, cancellationToken);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
