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
[Route("api/v1/account")]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _password;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly SecurityOptions _securityOptions;

    public AccountController(
        IUserRepository users, IRefreshTokenRepository tokens,
        IJwtService jwt, IPasswordService password, IEmailService email, IAuditService audit,
        IOptions<SecurityOptions> securityOptions)
    {
        _users = users;
        _tokens = tokens;
        _jwt = jwt;
        _password = password;
        _email = email;
        _audit = audit;
        _securityOptions = securityOptions.Value;
    }

    // ========== POST /api/v1/account/register ==========
    [HttpPost("register")]
    [EnableRateLimiting("auth-limit")]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _users.ExistsByUsernameAsync(req.Username))
            return Conflict(ApiErrorResponse.Create("USERNAME_TAKEN", "Username is already taken"));

        if (req.Email != null && await _users.ExistsByEmailAsync(req.Email))
            return Conflict(ApiErrorResponse.Create("EMAIL_TAKEN", "Email is already registered"));

        var hash = _password.HashPassword(req.Password);
        var userId = await _users.CreateAsync(req.Username, req.Email, hash);

        await _audit.LogAsync(userId, AuditActions.Register, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());

        return CreatedAtAction(nameof(Me), null,
            ApiResponse<object>.Success(
                new { Id = userId, req.Username, req.Email },
                "Registration successful"));
    }

    // ========== GET /api/v1/account/me ==========
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetByIdAsync(User.GetUserId());
        if (user == null)
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));

        return Ok(ApiResponse<MeResponse>.Success(
            new MeResponse(user.Id, user.Username, user.Email, user.Role)));
    }

    // ========== POST /api/v1/account/change-password ==========
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _users.GetByIdAsync(User.GetUserId());
        if (user == null)
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));

        if (!_password.VerifyPassword(req.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiErrorResponse.Create("WRONG_PASSWORD", "Current password is incorrect"));

        var newHash = _password.HashPassword(req.NewPassword);
        await _users.UpdatePasswordAsync(user.Id, newHash);
        await _tokens.RevokeAllByUserIdAsync(user.Id);
        Response.DeleteRefreshTokenCookie();

        await _audit.LogAsync(user.Id, AuditActions.ChangePassword, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());

        return Ok(ApiResponse<object>.Success(new { }, "Password changed. Please login again."));
    }

    // ========== POST /api/v1/account/forgot-password ==========
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-limit")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);

        if (user != null)
        {
            var resetToken = _jwt.GenerateResetToken();
            await _users.SetResetTokenAsync(user.Id, resetToken, DateTime.UtcNow.AddMinutes(_securityOptions.PasswordResetExpiryMinutes));
            await _email.SendPasswordResetEmailAsync(req.Email, resetToken, user.Username);
            await _audit.LogAsync(user.Id, AuditActions.ForgotPassword, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());
        }

        return Ok(ApiResponse<object>.Success(new { }, "If the email exists, a reset link has been sent."));
    }

    // ========== POST /api/v1/account/reset-password ==========
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-limit")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _users.GetByResetTokenAsync(req.Token);
        if (user == null)
            return BadRequest(ApiErrorResponse.Create("INVALID_TOKEN", "Reset token is invalid or expired"));

        var newHash = _password.HashPassword(req.NewPassword);
        await _users.ClearResetTokenAndUnlockAsync(user.Id, newHash);
        await _tokens.RevokeAllByUserIdAsync(user.Id);

        await _audit.LogAsync(user.Id, AuditActions.ResetPassword, HttpContext.GetClientIp(), HttpContext.GetClientUserAgent());

        return Ok(ApiResponse<object>.Success(new { }, "Password reset successful. Please login with your new password."));
    }
}
