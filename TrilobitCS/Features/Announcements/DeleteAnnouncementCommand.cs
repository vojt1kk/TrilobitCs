using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;

namespace TrilobitCS.Features.Announcements;

public record DeleteAnnouncementCommand(int AnnouncementId, int UserId) : IRequest;

public class DeleteAnnouncementHandler : IRequestHandler<DeleteAnnouncementCommand>
{
    private readonly AppDbContext _db;

    public DeleteAnnouncementHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var announcement = await _db.Announcements
            .Include(a => a.Organisation)
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, cancellationToken)
            ?? throw new NotFoundException("errors.announcement_not_found");

        if (announcement.Organisation.LeaderId != command.UserId)
            throw new ForbiddenException("errors.not_organisation_leader");

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
