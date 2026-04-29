using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;

namespace TrilobitCS.Features.Users;

public record LeaveOrganisationCommand(int UserId) : IRequest;

// Laravel: OrganisationController@leave
public class LeaveOrganisationHandler : IRequestHandler<LeaveOrganisationCommand>
{
    private readonly AppDbContext _db;

    public LeaveOrganisationHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(LeaveOrganisationCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([command.UserId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (user.OrganisationId is null)
            throw new ConflictException("errors.not_in_organisation");

        var isLeader = await _db.Organisations.AnyAsync(
            o => o.LeaderId == command.UserId, cancellationToken);

        if (isLeader)
            throw new ConflictException("errors.leader_cannot_leave");

        user.OrganisationId = null;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
