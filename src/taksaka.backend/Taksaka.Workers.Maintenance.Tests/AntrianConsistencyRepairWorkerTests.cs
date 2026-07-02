using Microsoft.Extensions.Logging.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Workers.Maintenance.Configuration;
using Taksaka.Workers.Maintenance.Models;
using Taksaka.Workers.Maintenance.Repository;
using Taksaka.Workers.Maintenance.Worker;

namespace Taksaka.Workers.Maintenance.Tests;

public sealed class AntrianConsistencyRepairWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsSuccessWithoutCallingRepo()
    {
        var repository = new FakeAntrianConsistencyRepairRepository();
        var worker = CreateWorker(repository, new AntrianConsistencyRepairOptions
        {
            WorkerEnabled = false
        });

        var result = await worker.ExecuteAsync(new Job());

        Assert.True(result.IsSuccess);
        Assert.Contains("disabled", result.Message!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, repository.FindCallCount);
        Assert.Equal(0, repository.RepairCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesAllRows_ContinuesOnFailure()
    {
        var repository = new FakeAntrianConsistencyRepairRepository
        {
            Items =
            [
                CreateItem("ANT-1", 1, "RG001"),
                CreateItem("ANT-2", 2, "RG002"),
                CreateItem("ANT-3", 3, "RG003")
            ],
            RepairHandler = (antrianId, _, _) =>
                antrianId == "ANT-2"
                    ? throw new InvalidOperationException("Simulated failure")
                    : Task.FromResult(1)
        };

        var worker = CreateWorker(repository);
        var result = await worker.ExecuteAsync(new Job());

        Assert.True(result.IsSuccess);
        Assert.Contains("Discovered=3", result.Message);
        Assert.Contains("Repaired=2", result.Message);
        Assert.Contains("Failed=1", result.Message);
        Assert.Equal(3, repository.RepairCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsInvalidRegistrationId()
    {
        var repository = new FakeAntrianConsistencyRepairRepository
        {
            Items =
            [
                CreateItem("ANT-1", 1, "RG001"),
                CreateItem("ANT-2", 2, " ")
            ]
        };

        var worker = CreateWorker(repository);
        var result = await worker.ExecuteAsync(new Job());

        Assert.True(result.IsSuccess);
        Assert.Contains("Repaired=1", result.Message);
        Assert.Contains("Skipped=1", result.Message);
        Assert.Equal(1, repository.RepairCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_LogsSummaryCounts()
    {
        var repository = new FakeAntrianConsistencyRepairRepository
        {
            Items =
            [
                CreateItem("ANT-1", 1, "RG001"),
                CreateItem("ANT-2", 2, "RG002", reffDesc: "REG", reffId: "RG002"),
                CreateItem("ANT-3", 3, "RG003")
            ],
            RepairHandler = (antrianId, _, _) => Task.FromResult(antrianId == "ANT-3" ? 0 : 1)
        };

        var worker = CreateWorker(repository);
        var result = await worker.ExecuteAsync(new Job());

        Assert.True(result.IsSuccess);
        Assert.Contains("Discovered=3", result.Message);
        Assert.Contains("Repaired=1", result.Message);
        Assert.Contains("Skipped=2", result.Message);
        Assert.Contains("Failed=0", result.Message);
        Assert.Contains("DurationMs=", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UsesPayloadBatchSizeWhenValid()
    {
        var repository = new FakeAntrianConsistencyRepairRepository();
        var worker = CreateWorker(repository, new AntrianConsistencyRepairOptions
        {
            BatchSize = 50
        });

        await worker.ExecuteAsync(new Job { Payload = """{"batchSize":25}""" });

        Assert.Equal(25, repository.LastBatchSize);
    }

    private static AntrianConsistencyRepairWorker CreateWorker(
        FakeAntrianConsistencyRepairRepository repository,
        AntrianConsistencyRepairOptions? options = null)
    {
        return new AntrianConsistencyRepairWorker(
            repository,
            options ?? new AntrianConsistencyRepairOptions(),
            NullLogger<AntrianConsistencyRepairWorker>.Instance);
    }

    private static AntrianConsistencyItem CreateItem(
        string antrianId,
        int noUrut,
        string registrationId,
        string reffDesc = "BOK",
        string? reffId = null) =>
        new()
        {
            AntrianId = antrianId,
            NoUrut = noUrut,
            ReffId = reffId ?? antrianId,
            ReffDesc = reffDesc,
            RegistrationId = registrationId
        };

    private sealed class FakeAntrianConsistencyRepairRepository : IAntrianConsistencyRepairRepository
    {
        public IReadOnlyList<AntrianConsistencyItem> Items { get; init; } = [];

        public Func<string, int, string, Task<int>> RepairHandler { get; init; } =
            (_, _, _) => Task.FromResult(1);

        public int FindCallCount { get; private set; }

        public int RepairCallCount { get; private set; }

        public int LastBatchSize { get; private set; }

        public Task<IReadOnlyList<AntrianConsistencyItem>> FindInconsistentAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            FindCallCount++;
            LastBatchSize = batchSize;
            return Task.FromResult(Items);
        }

        public Task<int> RepairAsync(
            string antrianId,
            int noUrut,
            string registrationId,
            CancellationToken cancellationToken)
        {
            RepairCallCount++;
            return RepairHandler(antrianId, noUrut, registrationId);
        }
    }
}
