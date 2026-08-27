namespace TrilobitCS.Responses;

public record AnnouncementAuthorResponse(int Id, string Nickname);

public record AnnouncementResponse(
    int Id,
    int OrganisationId,
    string Title,
    string Content,
    AnnouncementAuthorResponse CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
