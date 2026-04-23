using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetUserQuery(int UserId) : IRequest<UserResponse>;

// Laravel: UserController@show
public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    private readonly AppDbContext _db;

    public GetUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserResponse> Handle(GetUserQuery query, CancellationToken cancellationToken)
        => await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserResponse(
                u.Id,
                u.Nickname,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Gender,
                u.BirthDate,
                u.ProfilePicture,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");
}
