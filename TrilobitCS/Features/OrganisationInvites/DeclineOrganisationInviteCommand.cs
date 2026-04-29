using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.OrganisationInvites;

public record DeclineOrganisationInviteCommand(int UserId, int InviteId) : IRequest<OrganisationInviteResponse>;

public class DeclineOrganisationInviteHandler : IRequestHandler<DeclineOrganisationInviteCommand, OrganisationInviteResponse>
{
    private readonly AppDbContext _db;

    public DeclineOrganisationInviteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationInviteResponse> Handle(DeclineOrganisationInviteCommand command, CancellationToken cancellationToken)
    {
        var invite = await _db.OrganisationInvites
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(i => i.Id == command.InviteId && i.InvitedUserId == command.UserId, cancellationToken)
            ?? throw new NotFoundException("errors.invite_not_found");

        if (invite.Status != OrganisationInviteStatus.Pending)
            throw new ConflictException("errors.invite_not_pending");

        invite.Status = OrganisationInviteStatus.Declined;
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
