using System.Data;
using Taksaka.Core.Entities;

namespace Taksaka.Abstractions.Persistence;

public interface IQueueRepository
{
    Task EnqueueAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Job?> TryDequeueAsync(string lockOwner, TimeSpan lockDuration, CancellationToken cancellationToken = default);

    Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default);
}
