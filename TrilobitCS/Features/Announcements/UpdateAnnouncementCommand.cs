using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Announcements;

public record UpdateAnnouncementCommand(int AnnouncementId, int UserId, UpdateAnnouncementRequest Request) : IRequest<AnnouncementResponse>;

public class UpdateAnnouncementHandler : IRequestHandler<UpdateAnnouncementCommand, AnnouncementResponse>
{
    private readonly AppDbContext _db;

    public UpdateAnnouncementHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AnnouncementResponse> Handle(UpdateAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var announcement = await _db.Announcements
            .Include(a => a.Organisation)
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, cancellationToken)
            ?? throw new NotFoundException("errors.announcement_not_found");

        if (announcement.Organisation.LeaderId != command.UserId)
            throw new ForbiddenException("errors.not_organisation_leader");

        if (command.Request.Title is not null)
            announcement.Title = command.Request.Title;
        if (command.Request.Content is not null)
            announcement.Content = command.Request.Content;
        announcement.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new AnnouncementResponse(
            announcement.Id,
            announcement.OrganisationId,
            announcement.Title,
            announcement.Content,
            new AnnouncementAuthorResponse(announcement.CreatedBy.Id, announcement.CreatedBy.Nickname),
            announcement.CreatedAt,
            announcement.UpdatedAt
        );
    }
}
