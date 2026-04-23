using TrilobitCS.Models;

namespace TrilobitCS.Responses;

// Laravel: UserResource — veřejný tvar profilu uživatele (bez hashe hesla)
public record UserResponse(
    int Id,
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    Gender Gender,
    DateOnly BirthDate,
    string? ProfilePicture,
    DateTime CreatedAt
);
