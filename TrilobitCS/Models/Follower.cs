namespace TrilobitCS.Models;

// Laravel: pivot 'followers' (one-way follow)
public class Follower
{
    public int Id { get; set; }
    public int FollowerId { get; set; }
    public User FollowerUser { get; set; } = null!;
    public int FollowingId { get; set; }
    public User FollowingUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
