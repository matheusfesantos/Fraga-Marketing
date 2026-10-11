
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace Fraga.Api.Middleware;


public sealed class RedisRateLimitMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis)
{
    private const int Limit = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private const string IncrementScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return count
        """;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(ip)));

        var key = $"fraga:rate-limit:{hash}";
        var database = redis.GetDatabase();

        var result = await database.ScriptEvaluateAsync(
            IncrementScript,
            new RedisKey[] { key },
            new RedisValue[]
            {
                (long)Window.TotalMilliseconds
            });

        var count = (long)result;

        context.Response.Headers["X-RateLimit-Limit"] =
            Limit.ToString(CultureInfo.InvariantCulture);

        context.Response.Headers["X-RateLimit-Remaining"] =
            Math.Max(0, Limit - count)
                .ToString(CultureInfo.InvariantCulture);

        if (count > Limit)
        {
            context.Response.StatusCode =
                StatusCodes.Status429TooManyRequests;

            context.Response.Headers["Retry-After"] =
                ((int)Window.TotalSeconds).ToString(
                    CultureInfo.InvariantCulture);

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Limite de requisições excedido.",
                message = "Tente novamente em instantes."
            });

            return;
        }

        await next(context);
    }
}
