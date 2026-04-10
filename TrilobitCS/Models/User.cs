using TrilobitCS.Dto;

namespace TrilobitCS.Models;

// Laravel: App\Models\User
public class User
{
    public int Id { get; set; }
    public required string Nickname { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public Gender Gender { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }

    public static User FromDto(CreateUserDto dto) => new()
    {
        Nickname = dto.Nickname,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        Password = dto.HashedPassword,
        Gender = dto.Gender,
        BirthDate = dto.BirthDate,
        CreatedAt = DateTime.UtcNow,
    };
}
