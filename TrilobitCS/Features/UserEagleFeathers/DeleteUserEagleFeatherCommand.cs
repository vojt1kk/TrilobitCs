using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;

namespace TrilobitCS.Features.UserEagleFeathers;

public record DeleteUserEagleFeatherCommand(int UserId, int UserEagleFeatherId) : IRequest;

public class DeleteUserEagleFeatherHandler : IRequestHandler<DeleteUserEagleFeatherCommand>
{
    private readonly AppDbContext _db;

    public DeleteUserEagleFeatherHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteUserEagleFeatherCommand command, CancellationToken cancellationToken)
    {
        var uef = await _db.UserEagleFeathers.FindAsync([command.UserEagleFeatherId], cancellationToken)
            ?? throw new NotFoundException("errors.user_eagle_feather_not_found");

        if (uef.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        // Post.UserEagleFeather FK is Cascade, so the DB removes the attached Posts when the UEF
        // is removed below — but there is no FK from Like/Comment to Post (polymorphic, bare
        // int), so each Post's Likes and Comment tree (+ their likes) must be cleaned up
        // explicitly via CommentCascadeHelper before that cascade fires, or they'd be orphaned.
        var posts = await _db.Posts
            .Where(p => p.UserEagleFeatherId == uef.Id)
            .ToListAsync(cancellationToken);
        foreach (var post in posts)
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

        _db.UserEagleFeathers.Remove(uef);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
