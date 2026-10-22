namespace TrilobitCS.Responses;

public record UserProfileResponse
{
    public int Id { get; init; }
    public string Nickname { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? ProfilePicture { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsFollowing { get; init; }
    public int? FollowerCount { get; init; }
    public int? FollowingCount { get; init; }
    public int? ApprovedFeatherCount { get; init; }
    public int? PostCount { get; init; }
}
