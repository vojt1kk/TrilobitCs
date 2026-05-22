namespace TrilobitCS.Models;

public class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }
    public int UserEagleFeatherId { get; set; }
    public UserEagleFeather UserEagleFeather { get; set; } = null!;
    public int? ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
