using AuthAPI.Models;
using AuthAPI.Services;
using Dapper;

namespace AuthAPI.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByResetTokenAsync(string token);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
    Task<int> CreateAsync(string username, string? email, string passwordHash);
    Task UpdatePasswordAsync(int id, string passwordHash);
    Task UpdateLoginAttemptsAsync(int id, int attempts, DateTime? lockedUntil);
    Task ResetLoginAttemptsAsync(int id);
    Task SetResetTokenAsync(int id, string token, DateTime expires);
    Task ClearResetTokenAndUnlockAsync(int id, string passwordHash);
}

public class UserRepository : IUserRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public UserRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Username = @Username", new { Username = username });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Email = @Email", new { Email = email });
    }

    public async Task<User?> GetByResetTokenAsync(string token)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE PasswordResetToken = @Token AND PasswordResetExpires > @Now",
            new { Token = token, Now = DateTime.UtcNow });
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Users WHERE Username = @Username) THEN 1 ELSE 0 END AS BIT)",
            new { Username = username });
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Users WHERE Email = @Email) THEN 1 ELSE 0 END AS BIT)",
            new { Email = email });
    }

    public async Task<int> CreateAsync(string username, string? email, string passwordHash)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Users (Username, Email, PasswordHash)
              OUTPUT INSERTED.Id
              VALUES (@Username, @Email, @PasswordHash)",
            new { Username = username, Email = email, PasswordHash = passwordHash });
    }

    public async Task UpdatePasswordAsync(int id, string passwordHash)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id", new { Hash = passwordHash, Id = id });
    }

    public async Task UpdateLoginAttemptsAsync(int id, int attempts, DateTime? lockedUntil)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET LoginAttempts = @Attempts, LockedUntil = @LockedUntil WHERE Id = @Id",
            new { Attempts = attempts, LockedUntil = lockedUntil, Id = id });
    }

    public async Task ResetLoginAttemptsAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Users SET LoginAttempts = 0, LockedUntil = NULL WHERE Id = @Id", new { Id = id });
    }

    public async Task SetResetTokenAsync(int id, string token, DateTime expires)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET PasswordResetToken = @Token, PasswordResetExpires = @Expires WHERE Id = @Id",
            new { Token = token, Expires = expires, Id = id });
    }

    public async Task ClearResetTokenAndUnlockAsync(int id, string passwordHash)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Users SET PasswordHash = @Hash, PasswordResetToken = NULL, PasswordResetExpires = NULL,
              LoginAttempts = 0, LockedUntil = NULL WHERE Id = @Id",
            new { Hash = passwordHash, Id = id });
    }
}
