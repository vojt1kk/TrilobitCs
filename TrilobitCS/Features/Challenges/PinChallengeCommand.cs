using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Challenges;

public record PinChallengeCommand(int UserId, int ChallengeId) : IRequest<CurrentChallengeResponse>;

public class PinChallengeHandler : IRequestHandler<PinChallengeCommand, CurrentChallengeResponse>
{
    private readonly AppDbContext _db;

    public PinChallengeHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentChallengeResponse> Handle(PinChallengeCommand command, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Non-active challenges are reported as not found
        if (!await _db.Challenges.ActiveAt(now).AnyAsync(c => c.Id == command.ChallengeId, cancellationToken))
            throw new NotFoundException("errors.challenge_not_found");

        var pin = await _db.UserChallenges
            .FirstOrDefaultAsync(uc => uc.UserId == command.UserId && uc.ChallengeId == command.ChallengeId, cancellationToken);

        if (pin is null)
        {
            _db.UserChallenges.Add(new UserChallenge
            {
                UserId = command.UserId,
                ChallengeId = command.ChallengeId,
                PinnedAt = now,
            });
        }
        else if (pin.UnpinnedAt is null)
        {
            throw new ConflictException("errors.challenge_already_pinned");
        }
        else
        {
            pin.UnpinnedAt = null;
            pin.PinnedAt = now;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new ConflictException("errors.challenge_already_pinned");
        }

        return await _db.Challenges
            .Where(c => c.Id == command.ChallengeId)
            .ToCurrentResponse(command.UserId)
            .FirstAsync(cancellationToken);
    }
}
