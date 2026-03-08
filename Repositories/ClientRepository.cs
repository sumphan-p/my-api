using AuthAPI.Models;
using AuthAPI.Services;
using Dapper;

namespace AuthAPI.Repositories;

public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByClientIdAsync(string clientId);
    Task<bool> ExistsByClientIdAsync(string clientId);
    Task<bool> ExistsByClientIdExcludingAsync(string clientId, int excludeId);
    Task<int> CreateAsync(string clientId, string hashedSecret, string name);
    Task<bool> UpdateAsync(int id, string clientId, string name);
    Task<Client?> DisableAsync(int id);
}

public class ClientRepository : IClientRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public ClientRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<Client>(
            "SELECT Id, ClientId, Name, IsActive, CreatedAt FROM Clients ORDER BY CreatedAt DESC");
    }

    public async Task<Client?> GetByClientIdAsync(string clientId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Client>(
            "SELECT * FROM Clients WHERE ClientId = @ClientId AND IsActive = 1", new { ClientId = clientId });
    }

    public async Task<bool> ExistsByClientIdAsync(string clientId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Clients WHERE ClientId = @ClientId) THEN 1 ELSE 0 END AS BIT)",
            new { ClientId = clientId });
    }

    public async Task<bool> ExistsByClientIdExcludingAsync(string clientId, int excludeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Clients WHERE ClientId = @ClientId AND Id != @Id) THEN 1 ELSE 0 END AS BIT)",
            new { ClientId = clientId, Id = excludeId });
    }

    public async Task<int> CreateAsync(string clientId, string hashedSecret, string name)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Clients (ClientId, ClientSecret, Name)
              OUTPUT INSERTED.Id
              VALUES (@ClientId, @ClientSecret, @Name)",
            new { ClientId = clientId, ClientSecret = hashedSecret, Name = name });
    }

    public async Task<bool> UpdateAsync(int id, string clientId, string name)
    {
        using var conn = _connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE Clients SET ClientId = @ClientId, Name = @Name WHERE Id = @Id",
            new { ClientId = clientId, Name = name, Id = id });
        return affected > 0;
    }

    public async Task<Client?> DisableAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync("UPDATE Clients SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        if (affected == 0) return null;
        return await conn.QueryFirstOrDefaultAsync<Client>("SELECT * FROM Clients WHERE Id = @Id", new { Id = id });
    }
}
