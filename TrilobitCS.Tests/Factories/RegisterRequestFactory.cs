using Bogus;
using TrilobitCS.Models;
using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

// Laravel ekvivalent: database/factories/UserFactory.php
public static class RegisterRequestFactory
{
    private static readonly Faker _faker = new();

    // Laravel: UserFactory::new()->make()
    public static RegisterRequest Make() => new(
        Nickname: _faker.Random.AlphaNumeric(_faker.Random.Int(1, 20)),
        FirstName: _faker.Random.AlphaNumeric(_faker.Random.Int(1, 20)),
        LastName: _faker.Random.AlphaNumeric(_faker.Random.Int(1, 20)),
        Email: _faker.Internet.Email(),
        Password: "tajneheslo123",
        PasswordConfirm: "tajneheslo123",
        Gender: _faker.PickRandom<Gender>(),
        BirthDate: DateOnly.FromDateTime(_faker.Date.Past(20, DateTime.Now.AddYears(-10)))
    );
}
