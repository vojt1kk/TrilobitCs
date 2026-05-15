using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record GetOrganisationMembersQuery(int OrganisationId, PaginationQuery Pagination) : IRequest<PagedResponse<OrganisationMemberResponse>>;

public class GetOrganisationMembersHandler : IRequestHandler<GetOrganisationMembersQuery, PagedResponse<OrganisationMemberResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationMembersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<OrganisationMemberResponse>> Handle(GetOrganisationMembersQuery query, CancellationToken cancellationToken)
    {
        var exists = await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken);
        if (!exists)
            throw new NotFoundException("errors.organisation_not_found");

        var baseQuery = _db.Users
            .Where(u => u.OrganisationId == query.OrganisationId)
            .OrderBy(u => u.Id);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((query.Pagination.Page - 1) * query.Pagination.PageSize)
            .Take(query.Pagination.PageSize)
            .Select(u => new OrganisationMemberResponse(
                u.Id,
                u.Nickname,
                u.FirstName,
                u.LastName,
                u.ProfilePicture,
                u.Role,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrganisationMemberResponse>(
            items,
            query.Pagination.Page,
            query.Pagination.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / query.Pagination.PageSize));
    }
}
