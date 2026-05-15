using MediatR;
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

    public Task<PagedResponse<EagleFeatherResponse>> Handle(GetEagleFeathersQuery query, CancellationToken cancellationToken)
        => _db.EagleFeathers
            .OrderBy(f => f.Id)
            .ToPagedResponseAsync(query.Pagination, EagleFeatherResponse.FromModel, cancellationToken);
}
