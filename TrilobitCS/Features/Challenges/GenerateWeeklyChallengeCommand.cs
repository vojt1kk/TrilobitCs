using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Challenges;

public record GenerateWeeklyChallengeCommand(DateTime Now) : IRequest<int?>;

public class GenerateWeeklyChallengeHandler : IRequestHandler<GenerateWeeklyChallengeCommand, int?>
{
    private const long AdvisoryLockKey = 424242;
    private static readonly TimeSpan Period = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly ILogger<GenerateWeeklyChallengeHandler> _logger;

    public GenerateWeeklyChallengeHandler(AppDbContext db, ILogger<GenerateWeeklyChallengeHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<int?> Handle(GenerateWeeklyChallengeCommand command, CancellationToken cancellationToken)
        => _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Serialises concurrent instances; the trigger is re-evaluated inside the lock
            await _db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cancellationToken);

            var now = command.Now;

            // "Active" and "newest" are relative to the passed Now, not to the wall clock
            var newestActiveFrom = await _db.Challenges
                .ActiveAt(now)
                .OrderByDescending(c => c.ValidFrom).ThenByDescending(c => c.Id)
                .Select(c => c.ValidFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (newestActiveFrom is not null && newestActiveFrom > now - Period)
                return (int?)null;

            var feather = await _db.EagleFeathers
                .OrderBy(_ => EF.Functions.Random())
                .FirstOrDefaultAsync(cancellationToken);

            if (feather is null)
            {
                _logger.LogWarning("Weekly challenge not generated: eagle feather catalog is empty");
                return null;
            }

            var challenge = new Challenge
            {
                Title = feather.Name,
                Description = feather.Challenge,
                EagleFeatherId = feather.Id,
                DifficultyLevel = Random.Shared.Next(1, 4),
                ValidFrom = now,
                ValidTo = now + Period,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Challenges.Add(challenge);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return challenge.Id;
        });
}
