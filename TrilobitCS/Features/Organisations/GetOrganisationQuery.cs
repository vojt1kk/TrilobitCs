using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record GetOrganisationQuery(int OrganisationId) : IRequest<OrganisationResponse>;

// Laravel: OrganisationController@show
public class GetOrganisationHandler : IRequestHandler<GetOrganisationQuery, OrganisationResponse>
{
    private readonly AppDbContext _db;

    public GetOrganisationHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationResponse> Handle(GetOrganisationQuery query, CancellationToken cancellationToken)
        => await _db.Organisations
            .Where(o => o.Id == query.OrganisationId)
            .Select(o => new OrganisationResponse(
                o.Id,
                o.Name,
                o.Description,
                o.AvatarUrl,
                o.Members.Count,
                new OrganisationLeaderResponse(o.Leader.Id, o.Leader.Nickname),
                o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.organisation_not_found");
}
