using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Engine;
using Taksaka.Engine.DeadLetter;
using Taksaka.Engine.Queue;
using Taksaka.Engine.Retry;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Database;
using Taksaka.Infrastructure.Persistence.Repositories;

namespace Taksaka.Engine.Tests;

public sealed class EnginePersistenceFixture : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"taksaka-engine-test-{Guid.NewGuid():N}.db");

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
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
        services.AddSingleton<IQueue, QueueManager>();
        services.AddSingleton<IRetryManager, RetryManager>();
        services.AddSingleton<IDeadLetterManager, DeadLetterManager>();

        Services = services.BuildServiceProvider();
        await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }
}

public sealed class QueueManagerPersistenceTests : IClassFixture<EnginePersistenceFixture>
{
    private readonly IQueue _queue;
    private readonly IJobRepository _jobRepository;

    public QueueManagerPersistenceTests(EnginePersistenceFixture fixture)
    {
        _queue = fixture.Services.GetRequiredService<IQueue>();
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task EnqueueAsync_PersistsQueuedJob()
    {
        var job = new Job { Payload = "engine-queue" };
        await _queue.EnqueueAsync(job);

        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.Queued, loaded.Status);
        Assert.NotNull(loaded.EnqueuedAt);
    }
}

public sealed class RetryManagerPersistenceTests : IClassFixture<EnginePersistenceFixture>
{
    private readonly IRetryManager _retryManager;
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionHistoryRepository _historyRepository;

    public RetryManagerPersistenceTests(EnginePersistenceFixture fixture)
    {
        _retryManager = fixture.Services.GetRequiredService<IRetryManager>();
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
        _historyRepository = fixture.Services.GetRequiredService<IExecutionHistoryRepository>();
    }

    [Fact]
    public async Task ScheduleRetryAsync_PersistsRetryWaitingAndHistory()
    {
        var job = new Job { Payload = "retry-me" };
        await _jobRepository.InsertAsync(job);

        await _retryManager.ScheduleRetryAsync(job);

        var loaded = await _jobRepository.GetByIdAsync(job.Id);
        var history = await _historyRepository.GetByJobIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.RetryWaiting, loaded.Status);
        Assert.Equal(1, loaded.RetryCount);
        Assert.NotNull(loaded.NextRetryAt);
        Assert.NotEmpty(history);
    }
}

public sealed class DeadLetterManagerPersistenceTests : IClassFixture<EnginePersistenceFixture>
{
    private readonly IDeadLetterManager _deadLetterManager;
    private readonly IJobRepository _jobRepository;

    public DeadLetterManagerPersistenceTests(EnginePersistenceFixture fixture)
    {
        _deadLetterManager = fixture.Services.GetRequiredService<IDeadLetterManager>();
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_PersistsDeadLetterState()
    {
        var job = new Job { Payload = "failed" };
        await _jobRepository.InsertAsync(job);

        await _deadLetterManager.MoveToDeadLetterAsync(job, "max retries exceeded");

        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.DeadLetter, loaded.Status);
        Assert.Equal("max retries exceeded", loaded.DeadLetterReason);
    }
}
