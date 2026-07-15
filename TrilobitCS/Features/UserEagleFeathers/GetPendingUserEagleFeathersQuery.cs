using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.UserEagleFeathers;

public record GetPendingUserEagleFeathersQuery(int LeaderId, PaginationQuery Pagination)
    : IRequest<PagedResponse<UserEagleFeatherResponse>>;

public class GetPendingUserEagleFeathersHandler : IRequestHandler<GetPendingUserEagleFeathersQuery, PagedResponse<UserEagleFeatherResponse>>
{
    private readonly AppDbContext _db;

    public GetPendingUserEagleFeathersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<UserEagleFeatherResponse>> Handle(GetPendingUserEagleFeathersQuery query, CancellationToken cancellationToken)
    {
        var leader = await _db.Users.FindAsync([query.LeaderId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (leader.Role != UserRole.Leader)
            throw new ForbiddenException("errors.leader_only");

        return await _db.UserEagleFeathers
            .Where(uef => uef.Status == EagleFeatherStatus.Pending)
            .OrderBy(uef => uef.CreatedAt)
            .ThenBy(uef => uef.Id)
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
}
