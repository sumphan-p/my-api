using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthAPI.Models;
using AuthAPI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string GenerateResetToken();
    ClaimsPrincipal? ValidateToken(string token);
    object GetJwks();
    TokenValidationParameters GetTokenValidationParameters();
}

public class JwtService : IJwtService
{
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly string _keyId;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, IOptions<JwtOptions> jwtOptions, ILogger<JwtService> logger)
    {
        var opts = jwtOptions.Value;
        _issuer = !string.IsNullOrEmpty(opts.Issuer) ? opts.Issuer
            : throw new InvalidOperationException("Jwt:Issuer is not configured");
        _audience = !string.IsNullOrEmpty(opts.Audience) ? opts.Audience
            : throw new InvalidOperationException("Jwt:Audience is not configured");
        _accessTokenExpiryMinutes = opts.AccessTokenExpiryMinutes;
        _keyId = opts.KeyId;

        _logger = logger;

        var privateKeyBase64 = configuration["Jwt:PrivateKey"]
            ?? throw new InvalidOperationException("Jwt:PrivateKey is not configured. Use 'dotnet user-secrets set \"Jwt:PrivateKey\" \"<base64-key>\"'");

        _privateKey = RSA.Create();
        _privateKey.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

        _publicKey = RSA.Create();
        _publicKey.ImportRSAPublicKey(_privateKey.ExportRSAPublicKey(), out _);
    }

    public string GenerateAccessToken(User user)
    {
        var signingKey = new RsaSecurityKey(_privateKey) { KeyId = _keyId };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string GenerateResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToHexString(randomBytes).ToLowerInvariant();
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var validationParams = GetTokenValidationParameters();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, validationParams, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed");
            return null;
        }
    }

    public TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(_publicKey) { KeyId = _keyId },
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public object GetJwks()
    {
        var rsaParams = _publicKey.ExportParameters(false);
        return new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = _keyId,
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(rsaParams.Modulus!),
                    e = Base64UrlEncoder.Encode(rsaParams.Exponent!)
                }
            }
        };
    }
}
