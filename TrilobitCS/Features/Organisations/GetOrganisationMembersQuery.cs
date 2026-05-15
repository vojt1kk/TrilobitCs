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
        if (!await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken))
            throw new NotFoundException("errors.organisation_not_found");

        return await _db.Users
            .Where(u => u.OrganisationId == query.OrganisationId)
            .OrderBy(u => u.Id)
            .ToPagedResponseAsync(
                query.Pagination,
                u => new OrganisationMemberResponse(u.Id, u.Nickname, u.FirstName, u.LastName, u.ProfilePicture, u.Role, u.CreatedAt),
                cancellationToken);
    }
}
