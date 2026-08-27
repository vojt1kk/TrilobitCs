namespace TrilobitCS.Models;

public class Announcement
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
