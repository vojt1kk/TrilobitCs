using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Features.Shared;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Followers;

public record GetFollowingQuery(int UserId, int ViewerId, PaginationQuery Pagination) : IRequest<PagedResponse<PublicUserResponse>>;

public class GetFollowingHandler : IRequestHandler<GetFollowingQuery, PagedResponse<PublicUserResponse>>
{
    private readonly AppDbContext _db;

    public GetFollowingHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<PublicUserResponse>> Handle(GetFollowingQuery query, CancellationToken cancellationToken)
    {
        await PrivacyGuard.EnsureProfileVisibleAsync(_db, query.ViewerId, query.UserId, cancellationToken);

        // ToPagedResponseAsync applies its selector in-memory after ToListAsync, so it cannot
        // traverse an un-loaded navigation property — project FollowingUser server-side first.
        return await _db.Followers
            .Where(f => f.FollowerId == query.UserId)
            .OrderBy(f => f.Id)
            .Select(f => f.FollowingUser)
            .ToPagedResponseAsync(
                query.Pagination,
                u => new PublicUserResponse(u.Id, u.Nickname, u.FirstName, u.LastName, u.ProfilePicture, u.CreatedAt),
                cancellationToken);
    }
}
