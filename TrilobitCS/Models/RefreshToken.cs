namespace TrilobitCS.Models;

// Laravel: personal_access_tokens tabulka (Sanctum)
public class RefreshToken
{
    public int Id { get; set; }
    public required string Token { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsValid => !IsExpired && !IsRevoked;
}
