namespace TrilobitCS.Requests;

public record CreateOrganisationRequest(
    string Name,
    string? Description,
    string? AvatarUrl
);
