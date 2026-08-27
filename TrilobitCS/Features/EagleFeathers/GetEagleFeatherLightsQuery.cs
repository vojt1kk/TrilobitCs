using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;

namespace TrilobitCS.Features.EagleFeathers;

public record GetEagleFeatherLightsQuery : IRequest<int[]>;

public class GetEagleFeatherLightsHandler : IRequestHandler<GetEagleFeatherLightsQuery, int[]>
{
    private readonly AppDbContext _db;

    public GetEagleFeatherLightsHandler(AppDbContext db)
    {
        _db = db;
    }

    // Projected to int: System.Text.Json serializes byte[] as a base64 string, not a JSON array.
    public Task<int[]> Handle(GetEagleFeatherLightsQuery query, CancellationToken cancellationToken)
        => _db.EagleFeathers
            .Select(f => (int)f.Light)
            .Distinct()
            .OrderBy(l => l)
            .ToArrayAsync(cancellationToken);
}
