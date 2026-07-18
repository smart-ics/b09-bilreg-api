using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Database;
using Taksaka.Infrastructure.DependencyInjection;
using Taksaka.Infrastructure.Persistence.Repositories;

namespace Taksaka.Infrastructure.Tests;

public sealed class SqliteTestFixture : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"taksaka-test-{Guid.NewGuid():N}.db");

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
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<IExecutionHistoryRepository, ExecutionHistoryRepository>();
        services.AddSingleton<IAlertRepository, AlertRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

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

public sealed class DatabaseInitializerTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public DatabaseInitializerTests(SqliteTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        var initializer = _fixture.Services.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();
    }
}

public sealed class JobRepositoryTests : IClassFixture<SqliteTestFixture>
{
    private readonly IJobRepository _jobRepository;
    private readonly IQueueRepository _queueRepository;

    public JobRepositoryTests(SqliteTestFixture fixture)
    {
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
        _queueRepository = fixture.Services.GetRequiredService<IQueueRepository>();
    }

    [Fact]
    public async Task Insert_GetById_RoundTrip()
    {
        var job = new Job { Payload = "{\"action\":\"test\"}", Priority = JobPriority.High };

        await _jobRepository.InsertAsync(job);
        var loaded = await _jobRepository.GetByIdAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Equal(job.Payload, loaded.Payload);
        Assert.Equal(JobPriority.High, loaded.Priority);
    }

    [Fact]
    public async Task CreateReplayAsync_LinksSourceJob()
    {
        var source = new Job { Payload = "original" };
        await _jobRepository.InsertAsync(source);

        var replay = await _jobRepository.CreateReplayAsync(source.Id);

        Assert.Equal(source.Id, replay.SourceJobId);
        Assert.Equal(source.Payload, replay.Payload);
    }
}

public sealed class QueueRepositoryTests : IClassFixture<SqliteTestFixture>
{
    private readonly IJobRepository _jobRepository;
    private readonly IQueueRepository _queueRepository;

    public QueueRepositoryTests(SqliteTestFixture fixture)
    {
        _jobRepository = fixture.Services.GetRequiredService<IJobRepository>();
        _queueRepository = fixture.Services.GetRequiredService<IQueueRepository>();
    }

    [Fact]
    public async Task Enqueue_Dequeue_PersistsRunningState()
    {
        var job = new Job { Payload = "queued" };
        await _jobRepository.InsertAsync(job);
        await _queueRepository.EnqueueAsync(job);
        await _jobRepository.UpdateAsync(job);

        var dequeued = await _queueRepository.TryDequeueAsync("worker-a", TimeSpan.FromMinutes(5));

        Assert.NotNull(dequeued);
        Assert.Equal(JobStatus.Running, dequeued.Status);
        Assert.Equal("worker-a", dequeued.LockOwner);
    }

    [Fact]
    public async Task TryDequeueAsync_OnlyOneWorkerClaimsJob()
    {
        var job = new Job { Payload = "contended" };
        await _jobRepository.InsertAsync(job);
        await _queueRepository.EnqueueAsync(job);
        await _jobRepository.UpdateAsync(job);

        var first = await _queueRepository.TryDequeueAsync("worker-a", TimeSpan.FromMinutes(5));
        var second = await _queueRepository.TryDequeueAsync("worker-b", TimeSpan.FromMinutes(5));

        Assert.NotNull(first);
        Assert.Null(second);
    }
}

public sealed class ConfigurationRepositoryTests : IClassFixture<SqliteTestFixture>
{
    private readonly IConfigurationRepository _configurationRepository;

    public ConfigurationRepositoryTests(SqliteTestFixture fixture) =>
        _configurationRepository = fixture.Services.GetRequiredService<IConfigurationRepository>();

    [Fact]
    public async Task SetValueAsync_GetValueAsync_RoundTrip()
    {
        await _configurationRepository.SetValueAsync("Queue:LockLeaseSeconds", "120");
        var value = await _configurationRepository.GetValueAsync("Queue:LockLeaseSeconds");

        Assert.Equal("120", value);
    }
}
