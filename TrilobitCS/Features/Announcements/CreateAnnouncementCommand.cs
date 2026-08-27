using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Announcements;

public record CreateAnnouncementCommand(int UserId, int OrganisationId, CreateAnnouncementRequest Request) : IRequest<AnnouncementResponse>;

public class CreateAnnouncementHandler : IRequestHandler<CreateAnnouncementCommand, AnnouncementResponse>
{
    private readonly AppDbContext _db;

    public CreateAnnouncementHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AnnouncementResponse> Handle(CreateAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var org = await _db.Organisations
            .Include(o => o.Leader)
            .FirstOrDefaultAsync(o => o.Id == command.OrganisationId, cancellationToken)
            ?? throw new NotFoundException("errors.organisation_not_found");

        if (org.LeaderId != command.UserId)
            throw new ForbiddenException("errors.not_organisation_leader");

        var announcement = new Announcement
        {
            OrganisationId = command.OrganisationId,
            Title = command.Request.Title,
            Content = command.Request.Content,
            CreatedById = command.UserId,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(cancellationToken);

        return new AnnouncementResponse(
            announcement.Id,
            announcement.OrganisationId,
            announcement.Title,
            announcement.Content,
            new AnnouncementAuthorResponse(org.Leader.Id, org.Leader.Nickname),
            announcement.CreatedAt,
            announcement.UpdatedAt
        );
    }
}
