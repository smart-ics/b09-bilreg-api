using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Engine;

namespace Taksaka.Engine.Tests;

public sealed class EngineDependencyInjectionTests
{
    [Fact]
    public void AddTaksakaEngine_RegistersScheduler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IScheduleRepository, StubScheduleRepository>();
        services.AddSingleton<IJobRepository, StubJobRepository>();
        services.AddSingleton<IQueueRepository, StubQueueRepository>();
        services.AddTaksakaEngine();

        using var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IScheduler>();

        Assert.NotNull(scheduler);
    }

    private sealed class StubScheduleRepository : IScheduleRepository
    {
        public Task<IReadOnlyList<Schedule>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Schedule>>([]);

        public Task UpdateRunTimesAsync(Guid scheduleId, DateTimeOffset lastRunAt, DateTimeOffset nextRunAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InsertAsync(Schedule schedule, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubJobRepository : IJobRepository
    {
        public Task InsertAsync(Job job, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Job?>(null);

        public Task UpdateAsync(Job job, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Job> CreateReplayAsync(Guid sourceJobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Job());

        public Task<IReadOnlyList<Job>> GetRetryReadyAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Job>>([]);
    }

    private sealed class StubQueueRepository : IQueueRepository
    {
        public Task EnqueueAsync(Job job, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Job?> TryDequeueAsync(string lockOwner, TimeSpan lockDuration, CancellationToken cancellationToken = default) =>
            Task.FromResult<Job?>(null);

        public Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
