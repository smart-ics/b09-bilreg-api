using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Engine;

namespace Taksaka.Engine.Tests;

public sealed class EngineDependencyInjectionTests
{
    [Fact]
    public void AddTaksakaEngine_RegistersSchedulerAndDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IScheduleRepository, StubScheduleRepository>();
        services.AddSingleton<IJobRepository, StubJobRepository>();
        services.AddSingleton<IQueueRepository, StubQueueRepository>();
        services.AddSingleton<IExecutionHistoryRepository, StubExecutionHistoryRepository>();
        services.AddSingleton<IConfigurationRepository, StubConfigurationRepository>();
        services.AddSingleton<IAlertRepository, StubAlertRepository>();
        services.AddSingleton<IWorkerRegistry, StubWorkerRegistry>();
        services.AddTaksakaEngine();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IScheduler>());
        Assert.NotNull(provider.GetRequiredService<IDispatcher>());
        Assert.NotNull(provider.GetRequiredService<IHealthMonitor>());
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

    private sealed class StubExecutionHistoryRepository : IExecutionHistoryRepository
    {
        public Task InsertAsync(ExecutionHistory history, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ExecutionHistory>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExecutionHistory>>([]);
    }

    private sealed class StubConfigurationRepository : IConfigurationRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubAlertRepository : IAlertRepository
    {
        public Task InsertAsync(Alert alert, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Alert>>([]);
    }

    private sealed class StubWorkerRegistry : IWorkerRegistry
    {
        public bool TryGetWorker(string name, out IWorker? worker)
        {
            worker = null;
            return false;
        }

        public IReadOnlyCollection<string> RegisteredWorkerNames => [];

        public int RegisteredWorkerCount => 0;
    }
}
