using TrilobitCS.Models;

namespace TrilobitCS.Requests;

public record UpdateUserRequest(
    string Nickname,
    string FirstName,
    string LastName,
    Gender Gender,
    DateOnly BirthDate,
    string? ProfilePicture
);
