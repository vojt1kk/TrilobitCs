namespace TrilobitCS.Responses;

public record OrganisationLeaderResponse(int Id, string Nickname);

// Laravel: OrganisationResource
public record OrganisationResponse(
    int Id,
    string Name,
    string? Description,
    string? AvatarUrl,
    int MemberCount,
    OrganisationLeaderResponse Leader,
    DateTime CreatedAt
);
