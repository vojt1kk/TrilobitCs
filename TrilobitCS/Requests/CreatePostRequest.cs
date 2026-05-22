namespace TrilobitCS.Requests;

public record CreatePostRequest(
    string? Content,
    string? ImageUrl,
    int? OrganisationId,
    int? ChallengeId
);
