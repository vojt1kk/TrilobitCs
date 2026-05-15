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

    public Task<PagedResponse<OrganisationInviteResponse>> Handle(GetOrganisationInvitesQuery query, CancellationToken cancellationToken)
        => _db.OrganisationInvites
            .Include(i => i.InvitedUser)
            .Where(i => i.InvitedUserId == query.UserId)
            .OrderBy(i => i.Id)
            .ToPagedResponseAsync(
                query.Pagination,
                i => new OrganisationInviteResponse(i.Id, i.OrganisationId, i.InvitedUserId, i.InvitedUser.Nickname, i.InvitedById, i.Status, i.CreatedAt),
                cancellationToken);
}
