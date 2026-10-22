using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record UpdateUserCommand(int UserId, UpdateUserRequest Request) : IRequest<UserMeResponse>;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserMeResponse>
{
    private readonly AppDbContext _db;

    public UpdateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserMeResponse> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([command.UserId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        var request = command.Request;

        if (user.Nickname != request.Nickname &&
            await _db.Users.AnyAsync(u => u.Nickname == request.Nickname, cancellationToken))
            throw new ConflictException("errors.nickname_taken");

        user.Nickname = request.Nickname;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Gender = request.Gender;
        user.BirthDate = request.BirthDate;
        user.ProfilePicture = request.ProfilePicture;
        if (request.IsPrivate.HasValue)
            user.IsPrivate = request.IsPrivate.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Users
            .Where(u => u.Id == user.Id)
            .Select(GetCurrentUserHandler.Projection)
            .FirstAsync(cancellationToken);
    }
}
