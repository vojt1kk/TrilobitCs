using TrilobitCS.Models;

namespace TrilobitCS.Responses;

// Laravel: UserResource — vlastní profil (s emailem, role, organisationId)
public record UserMeResponse(
    int Id,
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    Gender Gender,
    DateOnly BirthDate,
    string? ProfilePicture,
    UserRole Role,
    int? OrganisationId,
    DateTime CreatedAt
);
