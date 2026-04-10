using TrilobitCS.Models;

namespace TrilobitCS.Requests;

// Laravel: App\Http\Requests\RegisterRequest
public record RegisterRequest(
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Gender Gender,
    DateOnly BirthDate
);
