namespace Taksaka.Core.Entities;

public sealed class ExecutionHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid JobId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;
}
