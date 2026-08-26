using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;

namespace TrilobitCS.Features.Comments;

public record DeleteCommentCommand(int CommentId, int UserId) : IRequest;

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly AppDbContext _db;

    public DeleteCommentHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await _db.Comments.FindAsync([command.CommentId], cancellationToken)
            ?? throw new NotFoundException("errors.comment_not_found");

        if (comment.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        var descendantIds = await CommentCascadeHelper.CollectDescendantCommentIdsAsync(_db, [command.CommentId], cancellationToken);
        var allIds = descendantIds.Append(command.CommentId).ToList();

        await CommentCascadeHelper.CascadeDeleteCommentsAndLikesAsync(_db, allIds, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
