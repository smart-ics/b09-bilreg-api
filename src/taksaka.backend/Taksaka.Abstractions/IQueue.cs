using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IQueue
{
    Task EnqueueAsync(Job job, CancellationToken cancellationToken = default);

    Task<Job?> DequeueAsync(CancellationToken cancellationToken = default);
}
