using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.OrganisationInvites;

public record GetOrganisationInvitesQuery(int UserId, PaginationQuery Pagination) : IRequest<PagedResponse<OrganisationInviteResponse>>;

public class GetOrganisationInvitesHandler : IRequestHandler<GetOrganisationInvitesQuery, PagedResponse<OrganisationInviteResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationInvitesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<OrganisationInviteResponse>> Handle(GetOrganisationInvitesQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.OrganisationInvites
            .Where(i => i.InvitedUserId == query.UserId)
            .OrderBy(i => i.Id);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((query.Pagination.Page - 1) * query.Pagination.PageSize)
            .Take(query.Pagination.PageSize)
            .Select(i => new OrganisationInviteResponse(
                i.Id,
                i.OrganisationId,
                i.InvitedUserId,
                i.InvitedUser.Nickname,
                i.InvitedById,
                i.Status,
                i.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrganisationInviteResponse>(
            items,
            query.Pagination.Page,
            query.Pagination.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / query.Pagination.PageSize));
    }
}
