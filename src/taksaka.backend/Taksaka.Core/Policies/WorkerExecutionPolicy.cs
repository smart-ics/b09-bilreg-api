namespace Taksaka.Core.Policies;

public sealed class WorkerExecutionPolicy
{
    public int MaxConcurrency { get; init; } = 1;

    public int? MaxRetryCount { get; init; }

    public TimeSpan? Timeout { get; init; }

    public bool CircuitBreakerEnabled { get; init; }
}
