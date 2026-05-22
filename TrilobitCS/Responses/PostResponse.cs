namespace TrilobitCS.Responses;

public record PostAuthorResponse(int Id, string Nickname, string? ProfilePicture);

public record PostResponse(
    int Id,
    PostAuthorResponse Author,
    int? OrganisationId,
    string? Content,
    string? ImageUrl,
    int UserEagleFeatherId,
    int? ChallengeId,
    int LikeCount,
    int CommentCount,
    DateTime CreatedAt
);
