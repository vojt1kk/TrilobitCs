using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;

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

        // Cascade (DB + EF config) removes the attached posts, and their likes/comments.
        _db.UserEagleFeathers.Remove(uef);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
