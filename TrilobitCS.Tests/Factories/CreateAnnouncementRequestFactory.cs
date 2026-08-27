using Bogus;
using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

public static class CreateAnnouncementRequestFactory
{
    private static readonly Faker _faker = new();

    public static CreateAnnouncementRequest Make()
    {
        var title = _faker.Lorem.Sentence(4);
        var content = _faker.Lorem.Sentence(15);
        return new(
            Title: title.Length > 60 ? title[..60] : title,
            Content: content.Length > 300 ? content[..300] : content
        );
    }
}
