namespace TrilobitCS.Models;

// One-way follow: follower tracks following, no mutual acceptance required.
public class Follower
{
    public int Id { get; set; }
    public int FollowerId { get; set; }
    public User FollowerUser { get; set; } = null!;
    public int FollowingId { get; set; }
    public User FollowingUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
