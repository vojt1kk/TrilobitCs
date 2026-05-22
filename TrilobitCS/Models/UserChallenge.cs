namespace TrilobitCS.Models;

public class UserChallenge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ChallengeId { get; set; }
    public DateTime PinnedAt { get; set; }
    public DateTime? UnpinnedAt { get; set; }
    public User User { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
}
