using TrilobitCS.Models;

namespace TrilobitCS.Requests;

// Laravel: App\Http\Requests\UpdateUserRequest
public record UpdateUserRequest(
    string Nickname,
    string FirstName,
    string LastName,
    Gender Gender,
    DateOnly BirthDate,
    string? ProfilePicture
);
