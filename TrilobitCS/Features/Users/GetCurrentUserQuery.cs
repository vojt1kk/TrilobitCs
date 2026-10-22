using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using System.Linq.Expressions;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetCurrentUserQuery(int UserId) : IRequest<UserMeResponse>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserMeResponse>
{
    private readonly AppDbContext _db;

    public GetCurrentUserHandler(AppDbContext db)
    {
        _db = db;
    }

    // Shared with UpdateUserHandler so both return the same shape from a single projection.
    public static readonly Expression<Func<User, UserMeResponse>> Projection = u => new UserMeResponse(
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
        u.CreatedAt,
        u.IsPrivate,
        u.Followers.Count,
        u.Following.Count,
        u.EagleFeathers.Count(f => f.Status == EagleFeatherStatus.Approved),
        u.Posts.Count);

    public async Task<UserMeResponse> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
        => await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");
}
