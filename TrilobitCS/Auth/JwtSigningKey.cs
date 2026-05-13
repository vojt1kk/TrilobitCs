namespace TrilobitCS.Auth;

// Shared signing key identifier — embedded in the JWT header ('kid') and in the
// validation key in Program.cs. Microsoft.IdentityModel 8+ strictly enforces that
// the token's 'kid' matches the validation key's KeyId.
internal static class JwtSigningKey
{
    public const string KeyId = "trilobit-default";
}
