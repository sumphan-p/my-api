using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.GetSub();

        if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("Invalid or missing user identity claim");

        return userId;
    }

    public static string? GetSub(this ClaimsPrincipal principal)
        => principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
           ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string? GetUsername(this ClaimsPrincipal principal)
        => principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
           ?? principal.FindFirst(ClaimTypes.Name)?.Value;

    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
           ?? principal.FindFirst(ClaimTypes.Email)?.Value;

    public static string? GetRole(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Role)?.Value;

    public static long? GetExp(this ClaimsPrincipal principal)
    {
        var exp = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        return exp != null ? long.Parse(exp) : null;
    }
}
