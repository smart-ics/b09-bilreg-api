using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.DeadLetter;

public sealed class DeadLetterManager(
    IJobRepository jobRepository,
    IExecutionHistoryRepository executionHistoryRepository) : IDeadLetterManager
{
    public async Task MoveToDeadLetterAsync(Job job, string reason, CancellationToken cancellationToken = default)
    {
        job.Status = JobStatus.DeadLetter;
        job.DeadLetterReason = reason;
        job.LockOwner = null;
        job.LockedUntil = null;

        await executionHistoryRepository.InsertAsync(new ExecutionHistory
        {
            JobId = job.Id,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Outcome = $"Dead letter: {reason}"
        }, cancellationToken);

        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
    }
}
