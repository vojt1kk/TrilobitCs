namespace TrilobitCS.Responses;

// Laravel: UserResource — veřejný profil (bez emailu)
public record PublicUserResponse(
    int Id,
    string Nickname,
    string FirstName,
    string LastName,
    string? ProfilePicture,
    DateTime CreatedAt
);
