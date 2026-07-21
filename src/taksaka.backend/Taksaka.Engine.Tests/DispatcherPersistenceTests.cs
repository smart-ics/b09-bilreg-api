using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Core.Policies;
using Taksaka.Engine.DeadLetter;
using Taksaka.Engine.Dispatching;
using Taksaka.Engine.Execution;
using Taksaka.Engine.Queue;
using Taksaka.Engine.Resources;
using Taksaka.Engine.Retry;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Database;
using Taksaka.Infrastructure.Persistence.Repositories;

namespace Taksaka.Engine.Tests;

public sealed class DispatcherPersistenceTests : IClassFixture<DispatcherPersistenceFixture>
{
    private readonly IDispatcher _dispatcher;
    private readonly IQueue _queue;
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionHistoryRepository _historyRepository;
    private readonly TestWorker _worker;

    public DispatcherPersistenceTests(DispatcherPersistenceFixture fixture)
    {
        _dispatcher = fixture.Services.GetRequiredService<IDispatcher>();
        _queue = fixture.Services.GetRequiredService<IQueue>();
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
        _historyRepository = fixture.Services.GetRequiredService<IExecutionHistoryRepository>();
        _worker = fixture.Worker;
    }

    [Fact]
    public async Task DispatchNextAsync_ExecutesRegisteredWorkerAndCompletesJob()
    {
        _worker.ShouldFail = false;
        _worker.Reset();
        var job = new Job { Payload = "{}", WorkerName = "test-worker" };
        await _queue.EnqueueAsync(job);

        await _dispatcher.DispatchNextAsync();

        var loaded = await _jobRepository.GetByIdAsync(job.Id);
        var history = await _historyRepository.GetByJobIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.Completed, loaded.Status);
        Assert.Equal(1, _worker.ExecutionCount);
        Assert.NotEmpty(history);
        Assert.Contains(history, entry => entry.Outcome.Contains("success", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DispatchNextAsync_MovesUnknownWorkerToDeadLetter()
    {
        _worker.ShouldFail = false;
        var job = new Job { Payload = "{}", WorkerName = "missing-worker" };
        await _queue.EnqueueAsync(job);

        await _dispatcher.DispatchNextAsync();

        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.DeadLetter, loaded.Status);
        Assert.Contains("not registered", loaded.DeadLetterReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DispatchNextAsync_SchedulesRetryOnWorkerFailure()
    {
        _worker.ShouldFail = true;
        var job = new Job { Payload = "{}", WorkerName = "test-worker" };
        await _queue.EnqueueAsync(job);

        await _dispatcher.DispatchNextAsync();

        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.RetryWaiting, loaded.Status);
        Assert.Equal(1, loaded.RetryCount);
    }

    [Fact]
    public async Task DispatchNextAsync_MovesToDeadLetterAfterMaxRetries()
    {
        _worker.ShouldFail = true;
        var job = new Job
        {
            Payload = "{}",
            WorkerName = "test-worker",
            RetryCount = 2
        };
        await _jobRepository.InsertAsync(job);
        job.Status = JobStatus.Queued;
        await _queue.EnqueueAsync(job);

        await _dispatcher.DispatchNextAsync();

        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.DeadLetter, loaded.Status);
    }
}

public sealed class DispatcherPersistenceFixture : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"taksaka-dispatch-test-{Guid.NewGuid():N}.db");

    public TestWorker Worker { get; } = new();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());

        services.AddOptions<DatabaseOptions>().Configure(options =>
        {
            options.Provider = DatabaseProvider.Sqlite;
            options.ConnectionString = $"Data Source={_databasePath}";
        });

        services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IQueueRepository, QueueRepository>();
        services.AddSingleton<IExecutionHistoryRepository, ExecutionHistoryRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IWorkerRegistry>(new StubWorkerRegistry(Worker));
        services.AddSingleton<IQueue, QueueManager>();
        services.AddSingleton<IDispatcher, Dispatcher>();
        services.AddSingleton<IResourceManager, ResourceManager>();
        services.AddSingleton<IRetryManager, RetryManager>();
        services.AddSingleton<IDeadLetterManager, DeadLetterManager>();
        services.AddSingleton<IEventPublisher, EventPublisher>();

        Services = services.BuildServiceProvider();
        await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
    }

    public Task DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    private sealed class StubWorkerRegistry(TestWorker worker) : IWorkerRegistry
    {
        public bool TryGetWorker(string name, out IWorker? resolvedWorker)
        {
            if (string.Equals(name, worker.Descriptor.Name, StringComparison.Ordinal))
            {
                resolvedWorker = worker;
                return true;
            }

            resolvedWorker = null;
            return false;
        }

        public IReadOnlyCollection<string> RegisteredWorkerNames => [worker.Descriptor.Name];

        public int RegisteredWorkerCount => 1;
    }
}

public sealed class TestWorker : IWorker
{
    public bool ShouldFail { get; set; }

    public int ExecutionCount { get; private set; }

    public void Reset() => ExecutionCount = 0;

    public WorkerDescriptor Descriptor { get; } = new()
    {
        Name = "test-worker",
        Category = WorkerCategory.Projection,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 2,
            MaxRetryCount = 2
        }
    };

    public Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(ShouldFail
            ? WorkerResult.Failure("Simulated worker failure")
            : WorkerResult.Success("Completed successfully"));
    }
}
