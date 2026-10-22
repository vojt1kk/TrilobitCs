using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Challenges;

public record UnpinChallengeCommand(int UserId, int ChallengeId) : IRequest<CurrentChallengeResponse>;

public class UnpinChallengeHandler : IRequestHandler<UnpinChallengeCommand, CurrentChallengeResponse>
{
    private readonly AppDbContext _db;

    public UnpinChallengeHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentChallengeResponse> Handle(UnpinChallengeCommand command, CancellationToken cancellationToken)
    {
        if (!await _db.Challenges.AnyAsync(c => c.Id == command.ChallengeId, cancellationToken))
            throw new NotFoundException("errors.challenge_not_found");

        var pin = await _db.UserChallenges
            .FirstOrDefaultAsync(uc => uc.UserId == command.UserId && uc.ChallengeId == command.ChallengeId
                && uc.UnpinnedAt == null, cancellationToken)
            ?? throw new ConflictException("errors.challenge_not_pinned");

        pin.UnpinnedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Challenges
            .Where(c => c.Id == command.ChallengeId)
            .ToCurrentResponse(command.UserId)
            .FirstAsync(cancellationToken);
    }
}
