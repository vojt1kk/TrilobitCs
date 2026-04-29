using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.OrganisationInvites;

public record AcceptOrganisationInviteCommand(int UserId, int InviteId) : IRequest<OrganisationInviteResponse>;

public class AcceptOrganisationInviteHandler : IRequestHandler<AcceptOrganisationInviteCommand, OrganisationInviteResponse>
{
    private readonly AppDbContext _db;

    public AcceptOrganisationInviteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationInviteResponse> Handle(AcceptOrganisationInviteCommand command, CancellationToken cancellationToken)
    {
        var invite = await _db.OrganisationInvites
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(i => i.Id == command.InviteId && i.InvitedUserId == command.UserId, cancellationToken)
            ?? throw new NotFoundException("errors.invite_not_found");

        if (invite.Status != OrganisationInviteStatus.Pending)
            throw new ConflictException("errors.invite_not_pending");

        if (invite.InvitedUser.OrganisationId is not null)
            throw new ConflictException("errors.user_already_in_organisation");

        invite.Status = OrganisationInviteStatus.Accepted;
        invite.InvitedUser.OrganisationId = invite.OrganisationId;

        // Auto-decline ostatní pending pozvánky tohoto uživatele
        await _db.OrganisationInvites
            .Where(i => i.InvitedUserId == command.UserId
                        && i.Id != command.InviteId
                        && i.Status == OrganisationInviteStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, OrganisationInviteStatus.Declined), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new OrganisationInviteResponse(
            invite.Id,
            invite.OrganisationId,
            invite.InvitedUserId,
            invite.InvitedUser.Nickname,
            invite.InvitedById,
            invite.Status,
            invite.CreatedAt
        );
    }
}
