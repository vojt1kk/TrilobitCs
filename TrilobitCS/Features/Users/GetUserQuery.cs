using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetUserQuery(int UserId) : IRequest<PublicUserResponse>;

public class GetUserHandler : IRequestHandler<GetUserQuery, PublicUserResponse>
{
    private readonly AppDbContext _db;

    public GetUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PublicUserResponse> Handle(GetUserQuery query, CancellationToken cancellationToken)
        => await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new PublicUserResponse(
                u.Id,
                u.Nickname,
                u.FirstName,
                u.LastName,
                u.ProfilePicture,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");
}
