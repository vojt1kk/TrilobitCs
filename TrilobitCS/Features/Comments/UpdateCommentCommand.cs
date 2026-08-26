using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Comments;

public record UpdateCommentCommand(int CommentId, int UserId, UpdateCommentRequest Request) : IRequest<CommentResponse>;

public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, CommentResponse>
{
    private readonly AppDbContext _db;

    public UpdateCommentHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommentResponse> Handle(UpdateCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await _db.Comments.FindAsync([command.CommentId], cancellationToken)
            ?? throw new NotFoundException("errors.comment_not_found");

        if (comment.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        comment.Content = command.Request.Content;

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Comments
            .Where(c => c.Id == comment.Id)
            .Select(c => new CommentResponse(
                c.Id,
                new PostAuthorResponse(c.User.Id, c.User.Nickname, c.User.ProfilePicture),
                c.CommentableType,
                c.CommentableId,
                c.Content,
                c.CreatedAt
            ))
            .FirstAsync(cancellationToken);
    }
}
