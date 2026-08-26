using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Followers;

public record GetFollowersQuery(int UserId, PaginationQuery Pagination) : IRequest<PagedResponse<PublicUserResponse>>;

public class GetFollowersHandler : IRequestHandler<GetFollowersQuery, PagedResponse<PublicUserResponse>>
{
    private readonly AppDbContext _db;

    public GetFollowersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<PublicUserResponse>> Handle(GetFollowersQuery query, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == query.UserId, cancellationToken))
            throw new NotFoundException("errors.user_not_found");

        // ToPagedResponseAsync applies its selector in-memory after ToListAsync, so it cannot
        // traverse an un-loaded navigation property — project FollowerUser server-side first.
        return await _db.Followers
            .Where(f => f.FollowingId == query.UserId)
            .OrderBy(f => f.Id)
            .Select(f => f.FollowerUser)
            .ToPagedResponseAsync(
                query.Pagination,
                u => new PublicUserResponse(u.Id, u.Nickname, u.FirstName, u.LastName, u.ProfilePicture, u.CreatedAt),
                cancellationToken);
    }
}
