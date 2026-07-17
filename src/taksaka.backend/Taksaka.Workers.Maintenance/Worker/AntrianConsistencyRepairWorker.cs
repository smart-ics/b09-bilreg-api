using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Taksaka.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Core.Policies;
using Taksaka.Workers.Maintenance.Composition;
using Taksaka.Workers.Maintenance.Configuration;
using Taksaka.Workers.Maintenance.Models;
using Taksaka.Workers.Maintenance.Repository;

namespace Taksaka.Workers.Maintenance.Worker;

public sealed class AntrianConsistencyRepairWorker : IWorker
{
    private readonly IAntrianConsistencyRepairRepository _repository;
    private readonly AntrianConsistencyRepairOptions _options;
    private readonly ILogger<AntrianConsistencyRepairWorker> _logger;

    public AntrianConsistencyRepairWorker()
        : this(CreateDependencies(PluginDirectoryResolver.Resolve()))
    {
    }

    private static (IAntrianConsistencyRepairRepository Repository, AntrianConsistencyRepairOptions Options, ILogger<AntrianConsistencyRepairWorker> Logger)
        CreateDependencies(string pluginDirectory)
    {
        var options = AntrianConsistencyRepairOptionsLoader.Load(pluginDirectory);
        var repository = new AntrianConsistencyRepairRepository(options);
        return (repository, options, NullLogger<AntrianConsistencyRepairWorker>.Instance);
    }

    internal AntrianConsistencyRepairWorker(
        IAntrianConsistencyRepairRepository repository,
        AntrianConsistencyRepairOptions options,
        ILogger<AntrianConsistencyRepairWorker> logger)
    {
        _repository = repository;
        _options = options;
        _logger = logger;

        Descriptor = new WorkerDescriptor
        {
            Name = "antrian-consistency-repair",
            Category = WorkerCategory.Maintenance,
            Policy = new WorkerExecutionPolicy
            {
                MaxConcurrency = 1,
                MaxRetryCount = null,
                Timeout = _options.ExecutionTimeout
            }
        };
    }

    private AntrianConsistencyRepairWorker(
        (IAntrianConsistencyRepairRepository Repository, AntrianConsistencyRepairOptions Options, ILogger<AntrianConsistencyRepairWorker> Logger) dependencies)
        : this(dependencies.Repository, dependencies.Options, dependencies.Logger)
    {
    }

    public WorkerDescriptor Descriptor { get; }

    public async Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (!_options.WorkerEnabled)
        {
            return WorkerResult.Success("Worker disabled.");
        }

        var batchSize = ResolveBatchSize(job);
        var stopwatch = Stopwatch.StartNew();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.ExecutionTimeout);

        try
        {
            _logger.LogInformation("Batch started. BatchSize={BatchSize}", batchSize);

            var items = await _repository.FindInconsistentAsync(batchSize, timeoutSource.Token);
            var discovered = items.Count;

            _logger.LogInformation("Rows discovered={RowsDiscovered}", discovered);

            var repaired = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var item in items)
            {
                if (timeoutSource.Token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    if (ShouldSkip(item))
                    {
                        skipped++;
                        continue;
                    }

                    var registrationId = item.RegistrationId!.Trim();
                    var rowsAffected = await _repository.RepairAsync(
                        item.AntrianId,
                        item.NoUrut,
                        registrationId,
                        timeoutSource.Token);

                    if (rowsAffected == 0)
                    {
                        skipped++;
                        continue;
                    }

                    repaired++;
                    _logger.LogInformation(
                        "Repaired AntrianId={AntrianId} NoUrut={NoUrut} OldReffId={OldReffId} NewRegistrationId={NewRegistrationId}",
                        item.AntrianId,
                        item.NoUrut,
                        item.ReffId,
                        registrationId);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    _logger.LogError(
                        ex,
                        "Failed to repair AntrianId={AntrianId} NoUrut={NoUrut}",
                        item.AntrianId,
                        item.NoUrut);
                }
            }

            stopwatch.Stop();
            var summary = FormatSummary(discovered, repaired, skipped, failed, stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Batch completed. RowsDiscovered={RowsDiscovered} RowsRepaired={RowsRepaired} RowsSkipped={RowsSkipped} RowsFailed={RowsFailed} DurationMs={DurationMs}",
                discovered,
                repaired,
                skipped,
                failed,
                stopwatch.ElapsedMilliseconds);

            if (timeoutSource.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return WorkerResult.Failure($"Execution timed out after {_options.ExecutionTimeout}. {summary}");
            }

            return WorkerResult.Success(summary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return WorkerResult.Failure("Execution was cancelled.");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return WorkerResult.Failure($"Execution timed out after {_options.ExecutionTimeout}.");
        }
    }

    private int ResolveBatchSize(Job job)
    {
        if (string.IsNullOrWhiteSpace(job.Payload))
        {
            return _options.BatchSize;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<AntrianConsistencyRepairJobPayload>(
                job.Payload,
                JsonSerializerOptions);
            if (payload?.BatchSize is >= 1)
            {
                return payload.BatchSize.Value;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid job payload; using configured BatchSize={BatchSize}", _options.BatchSize);
        }

        return _options.BatchSize;
    }

    private static bool ShouldSkip(AntrianConsistencyItem item)
    {
        if (string.IsNullOrWhiteSpace(item.RegistrationId))
        {
            return true;
        }

        return item.ReffDesc == "REG"
            && string.Equals(item.ReffId, item.RegistrationId.Trim(), StringComparison.Ordinal);
    }

    private static string FormatSummary(int discovered, int repaired, int skipped, int failed, long durationMs) =>
        $"Discovered={discovered}, Repaired={repaired}, Skipped={skipped}, Failed={failed}, DurationMs={durationMs}";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record AntrianConsistencyRepairJobPayload(int? BatchSize);
}
