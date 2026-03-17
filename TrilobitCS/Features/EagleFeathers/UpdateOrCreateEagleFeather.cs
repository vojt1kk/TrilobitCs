using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Features.EagleFeathers;

public record UpdateOrCreateEagleFeatherCommand(
    byte Light,
    string Section,
    short Number,
    string Name,
    string Challenge,
    string GrandChallenge,
    string SourceUrl
) : IRequest<EagleFeather>;

public class UpdateOrCreateEagleFeatherHandler : IRequestHandler<UpdateOrCreateEagleFeatherCommand, EagleFeather>
{
    private readonly AppDbContext _db;

    public UpdateOrCreateEagleFeatherHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EagleFeather> Handle(UpdateOrCreateEagleFeatherCommand request, CancellationToken cancellationToken)
    {
        var feather = await _db.EagleFeathers.FirstOrDefaultAsync(f =>
            f.Light == request.Light &&
            f.Section == request.Section &&
            f.Number == request.Number, cancellationToken);

        if (feather == null)
        {
            feather = new EagleFeather
            {
                Light = request.Light,
                Section = request.Section,
                Number = request.Number,
                Name = request.Name,
                Challenge = request.Challenge,
                GrandChallenge = request.GrandChallenge,
                SourceUrl = request.SourceUrl,
                CreatedAt = DateTime.UtcNow
            };
            _db.EagleFeathers.Add(feather);
        }

        feather.Name = request.Name;
        feather.Challenge = request.Challenge;
        feather.GrandChallenge = request.GrandChallenge;
        feather.SourceUrl = request.SourceUrl;
        feather.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return feather;
    }
}
