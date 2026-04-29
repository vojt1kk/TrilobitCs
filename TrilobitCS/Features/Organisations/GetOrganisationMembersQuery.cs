using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record GetOrganisationMembersQuery(int OrganisationId) : IRequest<IEnumerable<OrganisationMemberResponse>>;

// Laravel: OrganisationController@members
public class GetOrganisationMembersHandler : IRequestHandler<GetOrganisationMembersQuery, IEnumerable<OrganisationMemberResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationMembersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<OrganisationMemberResponse>> Handle(GetOrganisationMembersQuery query, CancellationToken cancellationToken)
    {
        var exists = await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken);
        if (!exists)
            throw new NotFoundException("errors.organisation_not_found");

        return await _db.Users
            .Where(u => u.OrganisationId == query.OrganisationId)
            .Select(u => new OrganisationMemberResponse(
                u.Id,
                u.Nickname,
                u.FirstName,
                u.LastName,
                u.ProfilePicture,
                u.Role,
                u.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
