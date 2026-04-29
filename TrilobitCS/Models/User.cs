namespace TrilobitCS.Models;

// Laravel: App\Models\User
public class User
{
    public int Id { get; set; }
    public required string Nickname { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public Gender Gender { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? ProfilePicture { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public int? OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }
    public DateTime CreatedAt { get; set; }

    // Laravel: hasMany relationships
    public ICollection<Follower> Following { get; set; } = new List<Follower>();
    public ICollection<Follower> Followers { get; set; } = new List<Follower>();
    public ICollection<UserEagleFeather> EagleFeathers { get; set; } = new List<UserEagleFeather>();
    public ICollection<ChallengeCompletion> ChallengeCompletions { get; set; } = new List<ChallengeCompletion>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<OrganisationInvite> ReceivedInvites { get; set; } = new List<OrganisationInvite>();
}
