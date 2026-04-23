namespace TrilobitCS.Models;

// Laravel: App\Models\Challenge
public class Challenge
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? EagleFeatherId { get; set; }
    public EagleFeather? EagleFeather { get; set; }
    public int? DifficultyLevel { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ChallengeCompletion> Completions { get; set; } = new List<ChallengeCompletion>();
}
