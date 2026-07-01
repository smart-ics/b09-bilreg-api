using Taksaka.Abstractions;
using Taksaka.Core.Entities;

namespace Taksaka.Engine.Queue;

public sealed class QueueManager : IQueue
{
    public Task EnqueueAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<Job?> DequeueAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<Job?>(null);
}
