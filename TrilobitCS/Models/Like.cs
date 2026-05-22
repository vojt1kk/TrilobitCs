namespace TrilobitCS.Models;

public class Like
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public LikeableType LikeableType { get; set; }
    public int LikeableId { get; set; }
    public DateTime CreatedAt { get; set; }
}
