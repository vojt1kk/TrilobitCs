using Bogus;
using TrilobitCS.Models;
using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

// Laravel ekvivalent: UserFactory pro update scénáře
public static class UpdateUserRequestFactory
{
    private static readonly Faker _faker = new();

    public static UpdateUserRequest Make() => new(
        Nickname: _faker.Random.AlphaNumeric(_faker.Random.Int(3, 20)),
        FirstName: _faker.Random.AlphaNumeric(_faker.Random.Int(3, 20)),
        LastName: _faker.Random.AlphaNumeric(_faker.Random.Int(3, 20)),
        Gender: _faker.PickRandom<Gender>(),
        BirthDate: DateOnly.FromDateTime(_faker.Date.Past(20, DateTime.Now.AddYears(-10))),
        ProfilePicture: null
    );
}
