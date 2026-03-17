using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public class EagleFeatherResponse
{
    public int Id { get; set; }
    public byte Light { get; set; }
    public string Section { get; set; } = "";
    public short Number { get; set; }
    public string Name { get; set; } = "";
    public string Challenge { get; set; } = "";
    public string GrandChallenge { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static EagleFeatherResponse FromModel(EagleFeather feather) => new()
    {
        Id = feather.Id,
        Light = feather.Light,
        Section = feather.Section,
        Number = feather.Number,
        Name = feather.Name,
        Challenge = feather.Challenge,
        GrandChallenge = feather.GrandChallenge,
        SourceUrl = feather.SourceUrl,
        CreatedAt = feather.CreatedAt,
        UpdatedAt = feather.UpdatedAt
    };
}
