using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetUserStatsQuery(int UserId, int ViewerId) : IRequest<UserStatsResponse>;

public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStatsResponse>
{
    private readonly AppDbContext _db;

    public GetUserStatsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserStatsResponse> Handle(GetUserStatsQuery query, CancellationToken cancellationToken)
    {
        await PrivacyGuard.EnsureProfileVisibleAsync(_db, query.ViewerId, query.UserId, cancellationToken);

        return await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserStatsResponse(
                u.Posts.Count,
                u.EagleFeathers.Count(f => f.Status == EagleFeatherStatus.Approved),
                u.Followers.Count,
                u.Following.Count))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");
    }
}
