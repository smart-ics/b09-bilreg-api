using Taksaka.Core.Enums;

namespace Taksaka.Core.Entities;

public sealed class Job
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Payload { get; init; } = string.Empty;

    public JobPriority Priority { get; init; } = JobPriority.Normal;

    public JobStatus Status { get; set; } = JobStatus.Created;

    public string? WorkerName { get; init; }

    public int RetryCount { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EnqueuedAt { get; set; }

    public DateTimeOffset? NextRetryAt { get; set; }

    public string? DeadLetterReason { get; set; }

    public Guid? SourceJobId { get; init; }

    public string? LockOwner { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }
}
