using Taksaka.Abstractions;
using Taksaka.Core.Entities;

namespace Taksaka.Engine.Resources;

public sealed class ResourceManager : IResourceManager
{
    public Task<ResourceDecision> TryAcquireAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.FromResult(ResourceDecision.Allow());
}
