using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Challenges;

internal static class ChallengeProjections
{
    public static IQueryable<CurrentChallengeResponse> ToCurrentResponse(this IQueryable<Challenge> query, int userId)
        => query.Select(c => new CurrentChallengeResponse(
            new ChallengeResponse(c.Id, c.Title, c.Description, c.EagleFeatherId, c.DifficultyLevel,
                c.ValidFrom, c.ValidTo, c.CreatedAt),
            c.UserChallenges.Any(uc => uc.UserId == userId && uc.UnpinnedAt == null),
            c.UserChallenges.Count(uc => uc.UnpinnedAt == null)));

    // Active at a given instant: ValidFrom <= now < ValidTo
    public static IQueryable<Challenge> ActiveAt(this IQueryable<Challenge> query, DateTime now)
        => query.Where(c => c.ValidFrom <= now && now < c.ValidTo);
}
