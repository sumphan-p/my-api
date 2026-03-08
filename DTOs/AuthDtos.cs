using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs;

public class LoginRequest
{
    [Required][StringLength(100)] public string Username { get; set; } = string.Empty;
    [Required][StringLength(72)] public string Password { get; set; } = string.Empty;
}

public record LoginResponse(
    string AccessToken,
    int ExpiresIn
);

public class IntrospectRequest
{
    [Required] public string Token { get; set; } = string.Empty;
}

public record IntrospectResponse(
    bool Active, string? Sub, string? Username, string? Email, string? Role, long? Exp);
