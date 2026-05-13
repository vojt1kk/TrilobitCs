using System.Security.Claims;

namespace TrilobitCS.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Relies on legacy JwtSecurityTokenHandler which maps 'sub' → ClaimTypes.NameIdentifier.
    // After migrating to JsonWebTokenHandler (planned), switch to JwtRegisteredClaimNames.Sub.
    public static int GetUserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
