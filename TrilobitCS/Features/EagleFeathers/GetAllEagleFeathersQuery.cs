using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.EagleFeathers;

public record GetAllEagleFeathersQuery : IRequest<EagleFeatherResponse[]>;

public class GetAllEagleFeathersHandler : IRequestHandler<GetAllEagleFeathersQuery, EagleFeatherResponse[]>
{
    private readonly AppDbContext _db;

    public GetAllEagleFeathersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EagleFeatherResponse[]> Handle(GetAllEagleFeathersQuery query, CancellationToken cancellationToken)
    {
        var feathers = await _db.EagleFeathers
            .OrderBy(f => f.Light)
            .ThenBy(f => f.Section)
            .ThenBy(f => f.Number)
            .ToListAsync(cancellationToken);

        return feathers.Select(EagleFeatherResponse.FromModel).ToArray();
    }
}
