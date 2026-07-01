using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.Queue;

public sealed class QueueManager(
    IJobRepository jobRepository,
    IQueueRepository queueRepository,
    IConfigurationRepository configurationRepository) : IQueue
{
    private const string LockLeaseConfigKey = "Queue:LockLeaseSeconds";
    private const int DefaultLockLeaseSeconds = 300;

    public async Task EnqueueAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (job.Status == JobStatus.Created)
        {
            await jobRepository.InsertAsync(job, cancellationToken: cancellationToken);
        }

        await queueRepository.EnqueueAsync(job, cancellationToken: cancellationToken);
        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    public async Task<Job?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var leaseSeconds = await GetLockLeaseSecondsAsync(cancellationToken);
        var lockOwner = Environment.MachineName;
        return await queueRepository.TryDequeueAsync(
            lockOwner,
            TimeSpan.FromSeconds(leaseSeconds),
            cancellationToken);
    }

    private async Task<int> GetLockLeaseSecondsAsync(CancellationToken cancellationToken)
    {
        var value = await configurationRepository.GetValueAsync(LockLeaseConfigKey, cancellationToken);
        return int.TryParse(value, out var seconds) && seconds > 0
            ? seconds
            : DefaultLockLeaseSeconds;
    }
}
