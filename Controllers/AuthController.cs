using AuthAPI.Extensions;
using AuthAPI.DTOs;
using AuthAPI.Models;
using AuthAPI.Options;
using AuthAPI.Repositories;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AuthAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IClientRepository _clients;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _password;
    private readonly IAuditService _audit;
    private readonly JwtOptions _jwtOptions;
    private readonly SecurityOptions _securityOptions;

    public AuthController(
        IUserRepository users, IRefreshTokenRepository tokens, IClientRepository clients,
        IJwtService jwt, IPasswordService password, IAuditService audit,
        IOptions<JwtOptions> jwtOptions, IOptions<SecurityOptions> securityOptions)
    {
        _users = users;
        _tokens = tokens;
        _clients = clients;
        _jwt = jwt;
        _password = password;
        _audit = audit;
        _jwtOptions = jwtOptions.Value;
        _securityOptions = securityOptions.Value;
    }

    // ========== POST /api/v1/auth/login ==========
    [HttpPost("login")]
    [EnableRateLimiting("auth-limit")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 429)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _users.GetByUsernameAsync(req.Username);

        if (user == null)
        {
            await _audit.LogAsync(null, AuditActions.FailedLogin, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent(), $"Unknown user: {req.Username}");
            return Unauthorized(ApiErrorResponse.Create("INVALID_CREDENTIALS", "Username or password is incorrect"));
        }

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            await _audit.LogAsync(user.Id, AuditActions.FailedLogin, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent(), "Account locked");
            return StatusCode(429, ApiErrorResponse.Create("ACCOUNT_LOCKED",
                $"Account is locked. Try again after {user.LockedUntil:yyyy-MM-dd HH:mm:ss} UTC"));
        }

        if (!_password.VerifyPassword(req.Password, user.PasswordHash))
        {
            var attempts = user.LoginAttempts + 1;
            DateTime? lockedUntil = attempts >= _securityOptions.MaxLoginAttempts
                ? DateTime.UtcNow.AddMinutes(_securityOptions.LockoutDurationMinutes)
                : null;
            await _users.UpdateLoginAttemptsAsync(user.Id, attempts, lockedUntil);
            await _audit.LogAsync(user.Id, AuditActions.FailedLogin, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent(),
                $"Attempt {attempts}/{_securityOptions.MaxLoginAttempts}");

            if (lockedUntil.HasValue)
                return StatusCode(429, ApiErrorResponse.Create("ACCOUNT_LOCKED",
                    $"Too many failed attempts. Account locked for {_securityOptions.LockoutDurationMinutes} minutes"));

            return Unauthorized(ApiErrorResponse.Create("INVALID_CREDENTIALS", "Username or password is incorrect"));
        }

        await _users.ResetLoginAttemptsAsync(user.Id);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        await _tokens.CreateAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays));
        Response.SetRefreshTokenCookie(refreshToken, _jwtOptions.RefreshTokenExpiryDays);

        await _audit.LogAsync(user.Id, AuditActions.Login, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());

        return Ok(ApiResponse<LoginResponse>.Success(
            new LoginResponse(accessToken, _jwtOptions.AccessTokenExpiryMinutes * 60),
            "Login successful"));
    }

    // ========== POST /api/v1/auth/refresh ==========
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[CookieNames.RefreshToken];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(ApiErrorResponse.Create("NO_REFRESH_TOKEN", "Refresh token is missing"));

        var storedToken = await _tokens.GetValidByTokenAsync(token);
        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(ApiErrorResponse.Create("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired"));

        await _tokens.RevokeByIdAsync(storedToken.Id);

        var user = await _users.GetByIdAsync(storedToken.UserId);
        if (user == null)
            return Unauthorized(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));

        var newAccessToken = _jwt.GenerateAccessToken(user);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        await _tokens.CreateAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays));
        Response.SetRefreshTokenCookie(newRefreshToken, _jwtOptions.RefreshTokenExpiryDays);

        return Ok(ApiResponse<LoginResponse>.Success(
            new LoginResponse(newAccessToken, _jwtOptions.AccessTokenExpiryMinutes * 60),
            "Token refreshed"));
    }

    // ========== POST /api/v1/auth/logout ==========
    [HttpPost("logout")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[CookieNames.RefreshToken];
        if (!string.IsNullOrEmpty(token))
        {
            var storedToken = await _tokens.GetByTokenAsync(token);
            await _tokens.RevokeByTokenAsync(token);

            if (storedToken != null)
                await _audit.LogAsync(storedToken.UserId, AuditActions.Logout, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());
        }

        Response.DeleteRefreshTokenCookie();
        return NoContent();
    }

    // ========== POST /api/v1/auth/introspect ==========
    [HttpPost("introspect")]
    [EnableRateLimiting("auth-limit")]
    [ProducesResponseType(typeof(IntrospectResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Introspect([FromBody] IntrospectRequest req)
    {
        var (clientId, clientSecret) = Request.GetClientCredentials();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return Unauthorized(ApiErrorResponse.Create("MISSING_CLIENT_CREDENTIALS", "X-Client-Id and X-Client-Secret headers are required"));

        var client = await _clients.GetByClientIdAsync(clientId);
        if (client == null || !_password.VerifyPassword(clientSecret, client.ClientSecret))
            return Unauthorized(ApiErrorResponse.Create("INVALID_CLIENT", "Invalid client credentials"));

        var principal = _jwt.ValidateToken(req.Token);
        if (principal == null)
            return Ok(new IntrospectResponse(false, null, null, null, null, null));

        return Ok(new IntrospectResponse(
            true,
            principal.GetSub(),
            principal.GetUsername(),
            principal.GetEmail(),
            principal.GetRole(),
            principal.GetExp()));
    }
}
