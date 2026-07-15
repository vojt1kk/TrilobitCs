using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.UserEagleFeathers;

public record GetMyUserEagleFeathersQuery(int UserId, PaginationQuery Pagination)
    : IRequest<PagedResponse<UserEagleFeatherResponse>>;

public class GetMyUserEagleFeathersHandler : IRequestHandler<GetMyUserEagleFeathersQuery, PagedResponse<UserEagleFeatherResponse>>
{
    private readonly AppDbContext _db;

    public GetMyUserEagleFeathersHandler(AppDbContext db)
    {
        _db = db;
    }

    public Task<PagedResponse<UserEagleFeatherResponse>> Handle(GetMyUserEagleFeathersQuery query, CancellationToken cancellationToken)
        => _db.UserEagleFeathers
            .Where(uef => uef.UserId == query.UserId)
            .OrderByDescending(uef => uef.CreatedAt)
            .ThenByDescending(uef => uef.Id)
            .ToPagedResponseAsync(
                query.Pagination,
                uef => new UserEagleFeatherResponse(
                    uef.Id,
                    uef.UserId,
                    uef.EagleFeatherId,
                    uef.IsGrandChallenge,
                    uef.IsCompleted,
                    uef.Status,
                    uef.VerifiedById,
                    uef.ModeratorNote,
                    uef.EarnedAt,
                    uef.CreatedAt),
                cancellationToken);
}
