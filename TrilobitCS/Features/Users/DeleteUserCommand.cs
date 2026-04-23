using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;

namespace TrilobitCS.Features.Users;

public record DeleteUserCommand(int UserId) : IRequest;

// Laravel: UserController@destroy
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

        // Smazání refresh tokenů před uživatelem (FK constraint bez cascade delete)
        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
