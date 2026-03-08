using System.Security.Cryptography;
using AuthAPI.Models;
using AuthAPI.Options;
using AuthAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthAPI.Tests.Services;

public class JwtServiceTests
{
    private readonly IJwtService _sut;

    public JwtServiceTests()
    {
        var rsa = RSA.Create(2048);
        var privateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:PrivateKey"] = privateKeyBase64,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:KeyId"] = "test-key"
            })
            .Build();

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiryMinutes = 15,
            KeyId = "test-key"
        });

        _sut = new JwtService(config, jwtOptions, NullLogger<JwtService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var user = new User { Id = 1, Username = "testuser", Email = "test@test.com", Role = "user" };

        var token = _sut.GenerateAccessToken(user);

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateAccessToken_CanBeValidated()
    {
        var user = new User { Id = 42, Username = "admin", Email = "admin@test.com", Role = "admin" };

        var token = _sut.GenerateAccessToken(user);
        var principal = _sut.ValidateToken(token);

        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 7, Username = "john", Email = "john@test.com", Role = "admin" };

        var token = _sut.GenerateAccessToken(user);
        var principal = _sut.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.Contains(principal.Claims, c =>
            c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == "7");
        Assert.Contains(principal.Claims, c =>
            c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "john");
        Assert.Contains(principal.Claims, c =>
            c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "admin");
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForInvalidToken()
    {
        var result = _sut.ValidateToken("invalid.jwt.token");

        Assert.Null(result);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.False(string.IsNullOrEmpty(token1));
    }

    [Fact]
    public void GenerateResetToken_ReturnsHexString()
    {
        var token = _sut.GenerateResetToken();

        Assert.Equal(64, token.Length); // 32 bytes = 64 hex chars
        Assert.True(token.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void GetJwks_ReturnsValidStructure()
    {
        var jwks = _sut.GetJwks();

        Assert.NotNull(jwks);
        var keysProperty = jwks.GetType().GetProperty("keys");
        Assert.NotNull(keysProperty);
    }

    [Fact]
    public void GetTokenValidationParameters_ReturnsConsistentParams()
    {
        var params1 = _sut.GetTokenValidationParameters();
        var params2 = _sut.GetTokenValidationParameters();

        Assert.Equal(params1.ValidIssuer, params2.ValidIssuer);
        Assert.Equal(params1.ValidAudience, params2.ValidAudience);
        Assert.True(params1.ValidateLifetime);
        Assert.True(params1.ValidateIssuerSigningKey);
    }
}
