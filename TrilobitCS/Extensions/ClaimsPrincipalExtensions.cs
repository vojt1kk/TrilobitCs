using System.Security.Claims;

namespace TrilobitCS.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Spoléhá na legacy JwtSecurityTokenHandler, který mapuje 'sub' → ClaimTypes.NameIdentifier.
    // Po migraci na JsonWebTokenHandler (plánováno) změnit na JwtRegisteredClaimNames.Sub.
    public static int GetUserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
