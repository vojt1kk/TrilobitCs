namespace TrilobitCS.Models;

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public CommentableType CommentableType { get; set; }
    public int CommentableId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
