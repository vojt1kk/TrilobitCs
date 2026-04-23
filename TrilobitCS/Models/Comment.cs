namespace TrilobitCS.Models;

// Laravel: App\Models\Comment (morphTo commentable)
public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public CommentableType CommentableType { get; set; }
    public int CommentableId { get; set; }
    public int? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
