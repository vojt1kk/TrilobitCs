using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record UpdateOrganisationCommand(int UserId, int OrganisationId, UpdateOrganisationRequest Request) : IRequest<OrganisationResponse>;

// Laravel: OrganisationController@update
public class UpdateOrganisationHandler : IRequestHandler<UpdateOrganisationCommand, OrganisationResponse>
{
    private readonly AppDbContext _db;

    public UpdateOrganisationHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationResponse> Handle(UpdateOrganisationCommand command, CancellationToken cancellationToken)
    {
        var org = await _db.Organisations
            .Include(o => o.Leader)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == command.OrganisationId, cancellationToken)
            ?? throw new NotFoundException("errors.organisation_not_found");

        if (org.LeaderId != command.UserId)
            throw new ForbiddenException("errors.not_organisation_leader");

        org.Name = command.Request.Name;
        org.Description = command.Request.Description;
        org.AvatarUrl = command.Request.AvatarUrl;

        await _db.SaveChangesAsync(cancellationToken);

        return new OrganisationResponse(
            org.Id,
            org.Name,
            org.Description,
            org.AvatarUrl,
            org.Members.Count,
            new OrganisationLeaderResponse(org.Leader.Id, org.Leader.Nickname),
            org.CreatedAt
        );
    }
}
