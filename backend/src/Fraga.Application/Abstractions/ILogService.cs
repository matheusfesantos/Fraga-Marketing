namespace Fraga.Application.Abstractions;

public interface ILogService
{
    void Information(string message, params object?[] args);

    void Warning(string message, params object?[] args);

    void Error(Exception exception, string message, params object?[] args);
}
