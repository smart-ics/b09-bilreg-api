using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.Retry;

public sealed class RetryManager(
    IJobRepository jobRepository,
    IExecutionHistoryRepository executionHistoryRepository,
    IConfigurationRepository configurationRepository) : IRetryManager
{
    private const string BaseDelayConfigKey = "Retry:BaseDelaySeconds";
    private const int DefaultBaseDelaySeconds = 30;

    public async Task ScheduleRetryAsync(Job job, CancellationToken cancellationToken = default)
    {
        job.RetryCount++;
        job.Status = JobStatus.RetryWaiting;

        var baseDelay = await GetBaseDelaySecondsAsync(cancellationToken);
        var delaySeconds = baseDelay * Math.Pow(2, job.RetryCount - 1);
        job.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        job.LockOwner = null;
        job.LockedUntil = null;

        await executionHistoryRepository.InsertAsync(new ExecutionHistory
        {
            JobId = job.Id,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Outcome = $"Retry scheduled (attempt {job.RetryCount})"
        }, cancellationToken);

        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private async Task<int> GetBaseDelaySecondsAsync(CancellationToken cancellationToken)
    {
        var value = await configurationRepository.GetValueAsync(BaseDelayConfigKey, cancellationToken);
        return int.TryParse(value, out var seconds) && seconds > 0
            ? seconds
            : DefaultBaseDelaySeconds;
    }
}
