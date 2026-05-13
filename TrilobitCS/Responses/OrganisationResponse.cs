namespace TrilobitCS.Responses;

public record OrganisationLeaderResponse(int Id, string Nickname);

public record OrganisationResponse(
    int Id,
    string Name,
    string? Description,
    string? AvatarUrl,
    int MemberCount,
    OrganisationLeaderResponse Leader,
    DateTime CreatedAt
);
