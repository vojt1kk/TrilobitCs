namespace TrilobitCS.Models;

public class EagleFeather
{
    public int Id { get; set; }
    public byte Light { get; set; }
    public required string Section { get; set; }
    public short Number { get; set; }
    public required string Name { get; set; }
    public required string Challenge { get; set; }
    public required string GrandChallenge { get; set; }
    public required string SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserEagleFeather> UserEagleFeathers { get; set; } = new List<UserEagleFeather>();
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
}
