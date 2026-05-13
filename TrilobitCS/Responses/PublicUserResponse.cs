namespace TrilobitCS.Responses;

public record PublicUserResponse(
    int Id,
    string Nickname,
    string FirstName,
    string LastName,
    string? ProfilePicture,
    DateTime CreatedAt
);
