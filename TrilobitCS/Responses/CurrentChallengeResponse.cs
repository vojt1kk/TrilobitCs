namespace TrilobitCS.Responses;

public record CurrentChallengeResponse(ChallengeResponse Challenge, bool IsPinned, int PinnedCount);
