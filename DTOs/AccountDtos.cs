using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs;

public class RegisterRequest
{
    [Required][StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required][StringLength(72, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required][Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required][StringLength(72)] public string CurrentPassword { get; set; } = string.Empty;

    [Required][StringLength(72, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required][Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; } = string.Empty;

    [Required][StringLength(72, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required][Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public record MeResponse(int Id, string Username, string? Email, string Role);
