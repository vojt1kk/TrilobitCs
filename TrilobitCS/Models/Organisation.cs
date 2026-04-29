namespace TrilobitCS.Models;

// Laravel: App\Models\Organisation
public class Organisation
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public int LeaderId { get; set; }
    public User Leader { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<OrganisationInvite> Invites { get; set; } = new List<OrganisationInvite>();
}
