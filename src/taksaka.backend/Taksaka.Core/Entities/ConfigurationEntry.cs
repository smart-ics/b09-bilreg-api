namespace Taksaka.Core.Entities;

public sealed class ConfigurationEntry
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
