namespace TrilobitCS.Models;

// Laravel: App\Models\ChallengeCompletion
public class ChallengeCompletion
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public decimal? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public DateTime CompletedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
