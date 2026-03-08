using AuthAPI.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuthAPI.HealthChecks;

public class DbHealthCheck : IHealthCheck
{
    private readonly IConnectionFactory _connectionFactory;

    public DbHealthCheck(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
