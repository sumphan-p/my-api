namespace AuthAPI.Extensions;

public static class HttpContextExtensions
{
    public static string? GetClientIp(this HttpContext context)
        => context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
           ?? context.Connection.RemoteIpAddress?.ToString();

    public static string? GetClientUserAgent(this HttpContext context)
        => context.Request.Headers.UserAgent.FirstOrDefault();

    public static (string? ClientId, string? ClientSecret) GetClientCredentials(this HttpRequest request)
        => (request.Headers[HttpHeaderNames.ClientId].FirstOrDefault(),
            request.Headers[HttpHeaderNames.ClientSecret].FirstOrDefault());

    public static void SetRefreshTokenCookie(this HttpResponse response, string token, int expiryDays)
    {
        response.Cookies.Append(CookieNames.RefreshToken, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(expiryDays)
        });
    }

    public static void DeleteRefreshTokenCookie(this HttpResponse response)
    {
        response.Cookies.Delete(CookieNames.RefreshToken);
    }
}
