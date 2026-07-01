using Taksaka.Core.Enums;

namespace Taksaka.Core.Entities;

public sealed class Job
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Payload { get; init; } = string.Empty;

    public JobPriority Priority { get; init; } = JobPriority.Normal;

    public JobStatus Status { get; set; } = JobStatus.Created;
}
