using Fraga.Application.Abstractions;

namespace Fraga.Api.Services;

public sealed class LogService(ILogger<LogService> logger) : ILogService
{
    public void Information(string message, params object?[] args)
    {
        logger.LogInformation(message, args);
    }

    public void Warning(string message, params object?[] args)
    {
        logger.LogWarning(message, args);
    }

    public void Error(Exception exception, string message, params object?[] args)
    {
        logger.LogError(exception, message, args);
    }
}
