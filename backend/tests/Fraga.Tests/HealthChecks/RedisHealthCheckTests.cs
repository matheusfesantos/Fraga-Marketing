using Fraga.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using StackExchange.Redis;

namespace Fraga.Tests.HealthChecks;

public sealed class RedisHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_RedisResponde_RetornaHealthy()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(redis => redis.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.Zero);

        var connection = new Mock<IConnectionMultiplexer>();
        connection
            .Setup(redis => redis.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(database.Object);

        var healthCheck = new RedisHealthCheck(connection.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_RedisFalha_RetornaUnhealthy()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(redis => redis.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis is unavailable."));

        var connection = new Mock<IConnectionMultiplexer>();
        connection
            .Setup(redis => redis.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(database.Object);

        var healthCheck = new RedisHealthCheck(connection.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis is unavailable.", result.Description);
    }
}
