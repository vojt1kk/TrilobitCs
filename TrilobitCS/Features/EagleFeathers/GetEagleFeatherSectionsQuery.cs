using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;

namespace TrilobitCS.Features.EagleFeathers;

public record GetEagleFeatherSectionsQuery(byte Light) : IRequest<string[]>;

public class GetEagleFeatherSectionsHandler : IRequestHandler<GetEagleFeatherSectionsQuery, string[]>
{
    private readonly AppDbContext _db;

    public GetEagleFeatherSectionsHandler(AppDbContext db)
    {
        _db = db;
    }

    public Task<string[]> Handle(GetEagleFeatherSectionsQuery query, CancellationToken cancellationToken)
        => _db.EagleFeathers
            .Where(f => f.Light == query.Light)
            .Select(f => f.Section)
            .Distinct()
            .OrderBy(s => s)
            .ToArrayAsync(cancellationToken);
}
