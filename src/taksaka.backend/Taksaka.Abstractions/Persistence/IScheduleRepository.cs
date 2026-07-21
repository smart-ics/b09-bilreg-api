using Taksaka.Core.Entities;

namespace Taksaka.Abstractions.Persistence;

public interface IScheduleRepository
{
    Task<IReadOnlyList<Schedule>> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task UpdateRunTimesAsync(Guid scheduleId, DateTimeOffset lastRunAt, DateTimeOffset nextRunAt, CancellationToken cancellationToken = default);

    Task InsertAsync(Schedule schedule, CancellationToken cancellationToken = default);
}
