using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;

namespace TrilobitCS.Features.Posts;

public record DeletePostCommand(int PostId, int UserId) : IRequest;

public class DeletePostHandler : IRequestHandler<DeletePostCommand>
{
    private readonly AppDbContext _db;

    public DeletePostHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeletePostCommand command, CancellationToken cancellationToken)
    {
        var post = await _db.Posts.FindAsync([command.PostId], cancellationToken)
            ?? throw new NotFoundException("errors.post_not_found");

        if (post.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        var uef = await _db.UserEagleFeathers.FindAsync([post.UserEagleFeatherId], cancellationToken);
        if (uef is not null)
            uef.IsCompleted = false;

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
