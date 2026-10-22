namespace TrilobitCS.Responses;

public record LeaderboardEntryResponse(int Rank, int UserId, string Nickname, string? ProfilePicture, int ApprovedFeatherCount);
