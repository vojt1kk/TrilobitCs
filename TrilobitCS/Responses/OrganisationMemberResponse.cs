using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public record OrganisationMemberResponse(
    int Id,
    string Nickname,
    string FirstName,
    string LastName,
    string? ProfilePicture,
    UserRole Role,
    DateTime CreatedAt
);
