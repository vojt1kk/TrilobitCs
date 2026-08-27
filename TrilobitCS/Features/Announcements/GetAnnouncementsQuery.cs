using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Announcements;

public record GetAnnouncementsQuery(int OrganisationId, int UserId, PaginationQuery Pagination) : IRequest<PagedResponse<AnnouncementResponse>>;

public class GetAnnouncementsHandler : IRequestHandler<GetAnnouncementsQuery, PagedResponse<AnnouncementResponse>>
{
    private readonly AppDbContext _db;

    public GetAnnouncementsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<AnnouncementResponse>> Handle(GetAnnouncementsQuery query, CancellationToken cancellationToken)
    {
        if (!await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken))
            throw new NotFoundException("errors.organisation_not_found");

        var callerOrganisationId = await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => u.OrganisationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (callerOrganisationId != query.OrganisationId)
            throw new ForbiddenException("errors.not_organisation_member");

        // Identity selector: the projection below (with the CreatedBy join) must run server-side,
        // so ToPagedResponseAsync's in-memory selector is just a pass-through.
        return await _db.Announcements
            .Where(a => a.OrganisationId == query.OrganisationId)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => new AnnouncementResponse(
                a.Id,
                a.OrganisationId,
                a.Title,
                a.Content,
                new AnnouncementAuthorResponse(a.CreatedBy.Id, a.CreatedBy.Nickname),
                a.CreatedAt,
                a.UpdatedAt
            ))
            .ToPagedResponseAsync(query.Pagination, x => x, cancellationToken);
    }
}
