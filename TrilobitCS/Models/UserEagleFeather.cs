namespace TrilobitCS.Models;

// Laravel: pivot user_eagle_feathers (workflow ziskani pera)
public class UserEagleFeather
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int EagleFeatherId { get; set; }
    public EagleFeather EagleFeather { get; set; } = null!;
    public bool IsGrandChallenge { get; set; }
    public EagleFeatherStatus Status { get; set; } = EagleFeatherStatus.Pending;
    public int? VerifiedById { get; set; }
    public User? VerifiedBy { get; set; }
    public DateTime? EarnedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
