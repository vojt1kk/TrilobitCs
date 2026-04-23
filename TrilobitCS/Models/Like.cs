namespace TrilobitCS.Models;

// Laravel: App\Models\Like (morphTo likeable)
public class Like
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public LikeableType LikeableType { get; set; }
    public int LikeableId { get; set; }
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
