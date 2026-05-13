using TrilobitCS.Models;

namespace TrilobitCS.Requests;

public record RegisterRequest(
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PasswordConfirm,
    Gender Gender,
    DateOnly BirthDate
);
