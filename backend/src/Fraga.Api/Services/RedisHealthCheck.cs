using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Fraga.Api.Services;

public sealed class RedisHealthCheck(
    IConnectionMultiplexer connectionMultiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connectionMultiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (RedisException exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}
