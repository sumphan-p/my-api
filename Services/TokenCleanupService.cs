using AuthAPI.Options;
using Dapper;
using Microsoft.Extensions.Options;

namespace AuthAPI.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TokenCleanupOptions _options;

    public TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger, IOptions<TokenCleanupOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupService started. Running every {Hours}h", _options.IntervalHours);

        await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Token cleanup failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("TokenCleanupService stopped.");
    }

    private async Task CleanupAsync()
    {
        using var scope = _services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        using var conn = connectionFactory.CreateConnection();

        var deletedTokens = await conn.ExecuteAsync(
            "DELETE FROM RefreshTokens WHERE IsRevoked = 1 OR ExpiresAt < @Now",
            new { Now = DateTime.UtcNow });

        var clearedResets = await conn.ExecuteAsync(
            "UPDATE Users SET PasswordResetToken = NULL, PasswordResetExpires = NULL WHERE PasswordResetExpires < @Now",
            new { Now = DateTime.UtcNow });

        var deletedLogs = await conn.ExecuteAsync(
            "DELETE FROM AuditLogs WHERE CreatedAt < @CutOff",
            new { CutOff = DateTime.UtcNow.AddDays(-_options.AuditLogRetentionDays) });

        _logger.LogInformation(
            "Cleanup done: {Tokens} tokens, {Resets} reset tokens, {Logs} audit logs removed",
            deletedTokens, clearedResets, deletedLogs);
    }
}
