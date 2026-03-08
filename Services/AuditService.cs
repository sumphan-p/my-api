using Dapper;

namespace AuthAPI.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string? ipAddress, string? userAgent, string? details = null);
}

public class AuditService : IAuditService
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IConnectionFactory connectionFactory, ILogger<AuditService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task LogAsync(int? userId, string action, string? ipAddress, string? userAgent, string? details = null)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                @"INSERT INTO AuditLogs (UserId, Action, IpAddress, UserAgent, Details)
                  VALUES (@UserId, @Action, @IpAddress, @UserAgent, @Details)",
                new { UserId = userId, Action = action, IpAddress = ipAddress, UserAgent = userAgent, Details = details });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log: {Action} for user {UserId}", action, userId);
        }
    }
}
