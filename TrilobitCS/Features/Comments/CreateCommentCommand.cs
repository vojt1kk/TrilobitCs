using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Comments;

public record CreateCommentCommand(int UserId, CommentableType Type, int TargetId, CreateCommentRequest Request) : IRequest<CommentResponse>;

public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentResponse>
{
    private readonly AppDbContext _db;

    public CreateCommentHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommentResponse> Handle(CreateCommentCommand command, CancellationToken cancellationToken)
    {
        if (!await PolymorphicTargetHelper.CommentableTargetExistsAsync(_db, command.Type, command.TargetId, cancellationToken))
            throw new NotFoundException("errors.commentable_target_not_found");

        var comment = new Comment
        {
            UserId = command.UserId,
            CommentableType = command.Type,
            CommentableId = command.TargetId,
            Content = command.Request.Content,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Comments.Add(comment);
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
