using Taksaka.Core.Enums;

namespace Taksaka.Core.Entities;

public sealed class HealthSnapshot
{
    public string Dimension { get; init; } = string.Empty;

    public HealthState State { get; init; } = HealthState.Unknown;

    public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.UtcNow;
}
