namespace TrilobitCS.Auth;

// Sdílený identifikátor podepisovacího klíče — vkládáme ho do JWT headeru ('kid')
// i do validačního klíče v Program.cs. Microsoft.IdentityModel (8+) striktně kontroluje
// shodu 'kid' tokenu s 'KeyId' validačního klíče.
internal static class JwtSigningKey
{
    public const string KeyId = "trilobit-default";
}
