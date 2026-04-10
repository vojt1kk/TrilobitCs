using TrilobitCS.Models;
using TrilobitCS.Requests;

namespace TrilobitCS.Dto;

public record CreateUserDto(
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    string HashedPassword,
    Gender Gender,
    DateOnly BirthDate
)
{
    public static CreateUserDto FromRequest(RegisterRequest request, string hashedPassword) => new(
        request.Nickname,
        request.FirstName,
        request.LastName,
        request.Email,
        hashedPassword,
        request.Gender,
        request.BirthDate
    );
}
