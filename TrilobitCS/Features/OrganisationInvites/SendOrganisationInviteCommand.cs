using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.OrganisationInvites;

public record SendOrganisationInviteCommand(int SenderId, SendOrganisationInviteRequest Request) : IRequest<OrganisationInviteResponse>;

public class SendOrganisationInviteHandler : IRequestHandler<SendOrganisationInviteCommand, OrganisationInviteResponse>
{
    private readonly AppDbContext _db;

    public SendOrganisationInviteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisationInviteResponse> Handle(SendOrganisationInviteCommand command, CancellationToken cancellationToken)
    {
        var sender = await _db.Users.FindAsync([command.SenderId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (sender.Role != UserRole.Leader || sender.OrganisationId is null)
            throw new ForbiddenException("errors.leader_only");

        var target = await _db.Users
            .FirstOrDefaultAsync(u => u.Nickname == command.Request.Nickname, cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (target.OrganisationId is not null)
            throw new ConflictException("errors.user_already_in_organisation");

        if (await _db.OrganisationInvites.AnyAsync(
                i => i.InvitedUserId == target.Id
                     && i.OrganisationId == sender.OrganisationId.Value
                     && i.Status == OrganisationInviteStatus.Pending,
                cancellationToken))
            throw new ConflictException("errors.invite_already_pending");

        var invite = new OrganisationInvite
        {
            OrganisationId = sender.OrganisationId.Value,
            InvitedUserId = target.Id,
            InvitedById = sender.Id,
            CreatedAt = DateTime.UtcNow,
        };

        _db.OrganisationInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);

        return new OrganisationInviteResponse(
            invite.Id,
            invite.OrganisationId,
            invite.InvitedUserId,
            target.Nickname,
            invite.InvitedById,
            invite.Status,
            invite.CreatedAt
        );
    }
}
