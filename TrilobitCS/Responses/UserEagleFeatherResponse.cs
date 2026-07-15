using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public record UserEagleFeatherResponse(
    int Id,
    int UserId,
    int EagleFeatherId,
    bool IsGrandChallenge,
    bool IsCompleted,
    EagleFeatherStatus Status,
    int? VerifiedById,
    string? ModeratorNote,
    DateTime? EarnedAt,
    DateTime CreatedAt);
