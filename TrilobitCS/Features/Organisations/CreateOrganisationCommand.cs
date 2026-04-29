using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record CreateOrganisationCommand(int UserId, CreateOrganisationRequest Request) : IRequest<OrganisationResponse>;

// Laravel: OrganisationController@store
public class CreateOrganisationHandler : IRequestHandler<CreateOrganisationCommand, OrganisationResponse>
{
    private readonly AppDbContext _db;

    public CreateOrganisationHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationResponse> Handle(CreateOrganisationCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([command.UserId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (user.Role != UserRole.Leader)
            throw new ForbiddenException("errors.leader_only");

        if (await _db.Organisations.AnyAsync(o => o.LeaderId == command.UserId, cancellationToken))
            throw new ConflictException("errors.organisation_already_exists");

        var org = new Organisation
        {
            Name = command.Request.Name,
            Description = command.Request.Description,
            AvatarUrl = command.Request.AvatarUrl,
            LeaderId = command.UserId,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Organisations.Add(org);
        await _db.SaveChangesAsync(cancellationToken);

        user.OrganisationId = org.Id;
        await _db.SaveChangesAsync(cancellationToken);

        return new OrganisationResponse(
            org.Id,
            org.Name,
            org.Description,
            org.AvatarUrl,
            1,
            new OrganisationLeaderResponse(user.Id, user.Nickname),
            org.CreatedAt
        );
    }
}
