using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record UpdateUserCommand(int UserId, UpdateUserRequest Request) : IRequest<UserResponse>;

// Laravel: UserController@update
public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserResponse>
{
    private readonly AppDbContext _db;

    public UpdateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserResponse> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
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

        await _db.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.Nickname,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Gender,
            user.BirthDate,
            user.ProfilePicture,
            user.CreatedAt
        );
    }
}
