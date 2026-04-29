namespace TrilobitCS.Models;

public class OrganisationInvite
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;
    public int InvitedUserId { get; set; }
    public User InvitedUser { get; set; } = null!;
    public int? InvitedById { get; set; }
    public User? InvitedBy { get; set; }
    public OrganisationInviteStatus Status { get; set; } = OrganisationInviteStatus.Pending;
    public DateTime CreatedAt { get; set; }
}
