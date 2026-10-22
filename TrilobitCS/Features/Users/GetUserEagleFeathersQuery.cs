using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetUserEagleFeathersQuery(int UserId, int ViewerId, PaginationQuery Pagination) : IRequest<PagedResponse<UserEagleFeatherResponse>>;

public class GetUserEagleFeathersHandler : IRequestHandler<GetUserEagleFeathersQuery, PagedResponse<UserEagleFeatherResponse>>
{
    private readonly AppDbContext _db;

    public GetUserEagleFeathersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<UserEagleFeatherResponse>> Handle(GetUserEagleFeathersQuery query, CancellationToken cancellationToken)
    {
        await PrivacyGuard.EnsureProfileVisibleAsync(_db, query.ViewerId, query.UserId, cancellationToken);

        // Only the owner sees pending/rejected attempts; everyone else sees earned feathers only.
        var isSelf = query.UserId == query.ViewerId;

        return await _db.UserEagleFeathers
            .Where(uef => uef.UserId == query.UserId && (isSelf || uef.Status == EagleFeatherStatus.Approved))
            .OrderByDescending(uef => uef.CreatedAt)
            .ThenByDescending(uef => uef.Id)
            .Select(uef => new UserEagleFeatherResponse(
                uef.Id,
                uef.UserId,
                uef.EagleFeatherId,
                uef.IsGrandChallenge,
                uef.IsCompleted,
                uef.Status,
                uef.VerifiedById,
                uef.ModeratorNote,
                uef.EarnedAt,
                uef.CreatedAt))
            .ToPagedResponseAsync(query.Pagination, x => x, cancellationToken);
    }
}
