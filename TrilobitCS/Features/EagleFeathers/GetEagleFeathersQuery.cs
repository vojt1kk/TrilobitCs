using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.EagleFeathers;

public record GetEagleFeathersQuery(PaginationQuery Pagination) : IRequest<PagedResponse<EagleFeatherResponse>>;

public class GetEagleFeathersHandler : IRequestHandler<GetEagleFeathersQuery, PagedResponse<EagleFeatherResponse>>
{
    private readonly AppDbContext _db;

    public GetEagleFeathersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<EagleFeatherResponse>> Handle(GetEagleFeathersQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.EagleFeathers.OrderBy(f => f.Id);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((query.Pagination.Page - 1) * query.Pagination.PageSize)
            .Take(query.Pagination.PageSize)
            .Select(f => EagleFeatherResponse.FromModel(f))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EagleFeatherResponse>(
            items,
            query.Pagination.Page,
            query.Pagination.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / query.Pagination.PageSize));
    }
}
