using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Challenges;

public record GetChallengeQuery(int ChallengeId) : IRequest<ChallengeResponse>;

public class GetChallengeHandler : IRequestHandler<GetChallengeQuery, ChallengeResponse>
{
    private readonly AppDbContext _db;

    public GetChallengeHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChallengeResponse> Handle(GetChallengeQuery query, CancellationToken cancellationToken)
        => await _db.Challenges
               .Where(c => c.Id == query.ChallengeId)
               .Select(c => new ChallengeResponse(c.Id, c.Title, c.Description, c.EagleFeatherId,
                   c.DifficultyLevel, c.ValidFrom, c.ValidTo, c.CreatedAt))
               .FirstOrDefaultAsync(cancellationToken)
           ?? throw new NotFoundException("errors.challenge_not_found");
}
