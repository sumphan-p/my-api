using AuthAPI.Models;
using AuthAPI.Services;
using Dapper;

namespace AuthAPI.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetValidByTokenAsync(string token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task CreateAsync(int userId, string token, DateTime expiresAt);
    Task RevokeByIdAsync(int id);
    Task RevokeByTokenAsync(string token);
    Task RevokeAllByUserIdAsync(int userId);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<RefreshToken?> GetValidByTokenAsync(string token)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RefreshToken>(
            "SELECT * FROM RefreshTokens WHERE Token = @Token AND IsRevoked = 0", new { Token = token });
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RefreshToken>(
            "SELECT * FROM RefreshTokens WHERE Token = @Token", new { Token = token });
    }

    public async Task CreateAsync(int userId, string token, DateTime expiresAt)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO RefreshTokens (UserId, Token, ExpiresAt) VALUES (@UserId, @Token, @ExpiresAt)",
            new { UserId = userId, Token = token, ExpiresAt = expiresAt });
    }

    public async Task RevokeByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE RefreshTokens SET IsRevoked = 1 WHERE Id = @Id", new { Id = id });
    }

    public async Task RevokeByTokenAsync(string token)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token", new { Token = token });
    }

    public async Task RevokeAllByUserIdAsync(int userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE RefreshTokens SET IsRevoked = 1 WHERE UserId = @UserId", new { UserId = userId });
    }
}
