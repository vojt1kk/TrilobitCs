using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetCurrentUserQuery(int UserId) : IRequest<UserMeResponse>;

// Laravel: UserController@me
public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserMeResponse>
{
    private readonly AppDbContext _db;

    public GetCurrentUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserMeResponse> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
        => await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserMeResponse(
                u.Id,
                u.Nickname,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Gender,
                u.BirthDate,
                u.ProfilePicture,
                u.Role,
                u.OrganisationId,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");
}
