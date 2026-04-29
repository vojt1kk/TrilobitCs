using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.OrganisationInvites;

public record GetOrganisationInvitesQuery(int UserId) : IRequest<IEnumerable<OrganisationInviteResponse>>;

public class GetOrganisationInvitesHandler : IRequestHandler<GetOrganisationInvitesQuery, IEnumerable<OrganisationInviteResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationInvitesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<OrganisationInviteResponse>> Handle(GetOrganisationInvitesQuery query, CancellationToken cancellationToken)
        => await _db.OrganisationInvites
            .Where(i => i.InvitedUserId == query.UserId)
            .Select(i => new OrganisationInviteResponse(
                i.Id,
                i.OrganisationId,
                i.InvitedUserId,
                i.InvitedUser.Nickname,
                i.InvitedById,
                i.Status,
                i.CreatedAt))
            .ToListAsync(cancellationToken);
}
