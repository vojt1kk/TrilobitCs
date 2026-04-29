namespace TrilobitCS.Requests;

public record UpdateOrganisationRequest(
    string Name,
    string? Description,
    string? AvatarUrl
);
