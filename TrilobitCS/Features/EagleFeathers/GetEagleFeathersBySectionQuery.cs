using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.EagleFeathers;

public record GetEagleFeathersBySectionQuery(byte Light, string Section) : IRequest<EagleFeatherResponse[]>;

public class GetEagleFeathersBySectionHandler : IRequestHandler<GetEagleFeathersBySectionQuery, EagleFeatherResponse[]>
{
    private readonly AppDbContext _db;

    public GetEagleFeathersBySectionHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EagleFeatherResponse[]> Handle(GetEagleFeathersBySectionQuery query, CancellationToken cancellationToken)
    {
        var feathers = await _db.EagleFeathers
            .Where(f => f.Light == query.Light && f.Section == query.Section)
            .OrderBy(f => f.Number)
            .ToListAsync(cancellationToken);

        return feathers.Select(EagleFeatherResponse.FromModel).ToArray();
    }
}
