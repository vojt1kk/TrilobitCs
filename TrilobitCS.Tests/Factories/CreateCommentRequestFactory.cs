using Bogus;
using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

public static class CreateCommentRequestFactory
{
    private static readonly Faker _faker = new();

    public static CreateCommentRequest Make(string? content = null)
        => new(content ?? _faker.Lorem.Sentence());
}
