using Taksaka.Abstractions;
using Taksaka.Core.Entities;

namespace Taksaka.Engine.Retry;

public sealed class RetryManager : IRetryManager
{
    public Task ScheduleRetryAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
