using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IRetryManager
{
    Task ScheduleRetryAsync(Job job, CancellationToken cancellationToken = default);
}
