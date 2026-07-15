using TrilobitCS.Requests;

namespace TrilobitCS.Tests.Factories;

public static class CreateUserEagleFeatherRequestFactory
{
    public static CreateUserEagleFeatherRequest Make(int eagleFeatherId, bool isGrandChallenge = false)
        => new(eagleFeatherId, isGrandChallenge);
}
