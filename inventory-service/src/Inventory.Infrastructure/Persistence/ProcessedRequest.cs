namespace Inventory.Infrastructure.Persistence;

public sealed class ProcessedRequest
{
    public string Key { get; init; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; init; }
}
