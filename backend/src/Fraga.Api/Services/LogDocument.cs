
namespace Fraga.Api.Services;

public sealed class LogDocument
{
    public DateTimeOffset Timestamp { get; init; } =
        DateTimeOffset.UtcNow;

    public string Level { get; init; } = string.Empty;

    public string MessageTemplate { get; init; } = string.Empty;

    public string RenderedMessage { get; init; } = string.Empty;

    public Dictionary<string, object?> Properties { get; init; } = new();
}
