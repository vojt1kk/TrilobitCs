namespace TrilobitCS.Responses;

public record ChallengeResponse(
    int Id,
    string Title,
    string? Description,
    int EagleFeatherId,
    int? DifficultyLevel,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    DateTime CreatedAt
);
