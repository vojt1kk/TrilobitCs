namespace TrilobitCS.Models;

// Laravel: App\Models\Post
public class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }
    public int? EagleFeatherId { get; set; }
    public EagleFeather? EagleFeather { get; set; }
    public int? ChallengeCompletionId { get; set; }
    public ChallengeCompletion? ChallengeCompletion { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
