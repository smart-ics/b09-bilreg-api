using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IResourceManager
{
    Task<ResourceDecision> TryAcquireAsync(Job job, CancellationToken cancellationToken = default);

    void Release(Job job);
}
