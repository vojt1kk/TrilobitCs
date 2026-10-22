using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record GetOrganisationLeaderboardQuery(int OrganisationId, PaginationQuery Pagination) : IRequest<PagedResponse<LeaderboardEntryResponse>>;

public class GetOrganisationLeaderboardHandler : IRequestHandler<GetOrganisationLeaderboardQuery, PagedResponse<LeaderboardEntryResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationLeaderboardHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<LeaderboardEntryResponse>> Handle(GetOrganisationLeaderboardQuery query, CancellationToken cancellationToken)
    {
        if (!await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken))
            throw new NotFoundException("errors.organisation_not_found");

        var page = await _db.Users
            .Where(u => u.OrganisationId == query.OrganisationId)
            .Select(u => new
            {
                UserId = u.Id,
                u.Nickname,
                u.ProfilePicture,
                Count = u.EagleFeathers.Count(f => f.Status == EagleFeatherStatus.Approved),
            })
            .OrderByDescending(x => x.Count).ThenBy(x => x.UserId)
            .ToPagedResponseAsync(query.Pagination, x => x, cancellationToken);

        // Rank depends on the page offset, so it is assigned after the page is materialised.
        var offset = (query.Pagination.Page - 1) * query.Pagination.PageSize;
        var items = page.Items
            .Select((x, i) => new LeaderboardEntryResponse(offset + i + 1, x.UserId, x.Nickname, x.ProfilePicture, x.Count))
            .ToList();

        return new PagedResponse<LeaderboardEntryResponse>(items, page.Page, page.PageSize, page.TotalCount, page.TotalPages);
    }
}
