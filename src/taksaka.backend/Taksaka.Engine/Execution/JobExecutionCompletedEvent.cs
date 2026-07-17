namespace Taksaka.Engine.Execution;

public sealed class JobExecutionCompletedEvent
{
    public required Guid JobId { get; init; }

    public required string WorkerName { get; init; }

    public required bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
