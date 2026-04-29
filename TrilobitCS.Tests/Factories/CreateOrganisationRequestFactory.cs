using Bogus;
using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

public static class CreateOrganisationRequestFactory
{
    private static readonly Faker _faker = new();

    public static CreateOrganisationRequest Make()
    {
        var name = _faker.Company.CompanyName();
        return new(
            Name: name.Length > 100 ? name[..100] : name,
            Description: _faker.Lorem.Sentence(),
            AvatarUrl: _faker.Internet.Avatar()
        );
    }
}
