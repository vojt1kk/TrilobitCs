using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Challenges;

public record GetCurrentChallengeQuery(int UserId) : IRequest<CurrentChallengeResponse>;

public class GetCurrentChallengeHandler : IRequestHandler<GetCurrentChallengeQuery, CurrentChallengeResponse>
{
    private readonly AppDbContext _db;

    public GetCurrentChallengeHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentChallengeResponse> Handle(GetCurrentChallengeQuery query, CancellationToken cancellationToken)
        => await _db.Challenges
               .ActiveAt(DateTime.UtcNow)
               .OrderByDescending(c => c.ValidFrom).ThenByDescending(c => c.Id)
               .ToCurrentResponse(query.UserId)
               .FirstOrDefaultAsync(cancellationToken)
           ?? throw new NotFoundException("errors.challenge_not_found");
}
