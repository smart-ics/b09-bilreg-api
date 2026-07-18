using Microsoft.Extensions.Logging;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Engine.Execution;

namespace Taksaka.Engine.Dispatching;

public sealed class Dispatcher(
    IQueue queue,
    IWorkerRegistry workerRegistry,
    IJobRepository jobRepository,
    IExecutionHistoryRepository executionHistoryRepository,
    IRetryManager retryManager,
    IDeadLetterManager deadLetterManager,
    IResourceManager resourceManager,
    IEventPublisher eventPublisher,
    ILogger<Dispatcher> logger) : IDispatcher
{
    public async Task DispatchNextAsync(CancellationToken cancellationToken = default)
    {
        var job = await queue.DequeueAsync(cancellationToken);
        if (job is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(job.WorkerName))
        {
            await deadLetterManager.MoveToDeadLetterAsync(
                job,
                "Job has no worker name assigned. Set WorkerName when creating the job.",
                cancellationToken);
            return;
        }

        if (!workerRegistry.TryGetWorker(job.WorkerName, out var worker) || worker is null)
        {
            await deadLetterManager.MoveToDeadLetterAsync(
                job,
                $"Worker '{job.WorkerName}' is not registered. " +
                $"Registered workers: [{string.Join(", ", workerRegistry.RegisteredWorkerNames)}]. " +
                "Ensure the plugin DLL and manifest are in the plugins folder and restart the server.",
                cancellationToken);
            return;
        }

        var resourceDecision = await resourceManager.TryAcquireAsync(job, cancellationToken);
        if (!resourceDecision.CanExecute)
        {
            logger.LogDebug(
                "Concurrency limit reached for worker {WorkerName}, re-queuing job {JobId}: {Reason}",
                job.WorkerName,
                job.Id,
                resourceDecision.Reason);
            await queue.EnqueueAsync(job, cancellationToken);
            return;
        }

        var startedAt = DateTimeOffset.UtcNow;
        WorkerResult result;

        try
        {
            result = await ExecuteWithTimeoutAsync(worker, job, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            result = WorkerResult.Failure(
                $"Worker '{job.WorkerName}' exceeded its configured execution timeout.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Worker '{WorkerName}' threw an unhandled exception for job {JobId}",
                job.WorkerName,
                job.Id);
            result = WorkerResult.Failure($"Unhandled exception: {ex.Message}");
        }
        finally
        {
            resourceManager.Release(job);
        }

        if (result.IsSuccess)
        {
            await CompleteJobAsync(job, worker, startedAt, result.Message, cancellationToken);
        }
        else
        {
            await HandleFailureAsync(job, worker, startedAt, result.Message ?? "Worker returned failure.", cancellationToken);
        }
    }

    private static async Task<WorkerResult> ExecuteWithTimeoutAsync(
        IWorker worker,
        Job job,
        CancellationToken cancellationToken)
    {
        var timeout = worker.Descriptor.Policy.Timeout;
        if (timeout is null)
        {
            return await worker.ExecuteAsync(job, cancellationToken);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);
        return await worker.ExecuteAsync(job, timeoutCts.Token);
    }

    private async Task CompleteJobAsync(
        Job job,
        IWorker worker,
        DateTimeOffset startedAt,
        string? message,
        CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        job.Status = JobStatus.Completed;
        job.LockOwner = null;
        job.LockedUntil = null;

        await executionHistoryRepository.InsertAsync(new ExecutionHistory
        {
            JobId = job.Id,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Outcome = message ?? "Completed successfully"
        }, cancellationToken);

        await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);

        logger.LogInformation(
            "Job {JobId} completed by worker '{WorkerName}' in {DurationMs}ms",
            job.Id,
            worker.Descriptor.Name,
            (completedAt - startedAt).TotalMilliseconds);

        await eventPublisher.PublishAsync(new JobExecutionCompletedEvent
        {
            JobId = job.Id,
            WorkerName = worker.Descriptor.Name,
            IsSuccess = true,
            Message = message,
            CompletedAt = completedAt
        }, cancellationToken);
    }

    private async Task HandleFailureAsync(
        Job job,
        IWorker worker,
        DateTimeOffset startedAt,
        string reason,
        CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var maxRetries = worker.Descriptor.Policy.MaxRetryCount;

        await executionHistoryRepository.InsertAsync(new ExecutionHistory
        {
            JobId = job.Id,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Outcome = $"Failed: {reason}"
        }, cancellationToken);

        if (maxRetries.HasValue && job.RetryCount >= maxRetries.Value)
        {
            await deadLetterManager.MoveToDeadLetterAsync(
                job,
                $"Max retry count ({maxRetries.Value}) exceeded. Last error: {reason}",
                cancellationToken);

            logger.LogWarning(
                "Job {JobId} moved to dead letter after {RetryCount} retries (worker '{WorkerName}')",
                job.Id,
                job.RetryCount,
                worker.Descriptor.Name);
        }
        else
        {
            job.Status = JobStatus.Failed;
            job.LockOwner = null;
            job.LockedUntil = null;
            await retryManager.ScheduleRetryAsync(job, cancellationToken);

            logger.LogWarning(
                "Job {JobId} failed on worker '{WorkerName}', scheduled for retry {RetryCount}: {Reason}",
                job.Id,
                worker.Descriptor.Name,
                job.RetryCount,
                reason);
        }

        await eventPublisher.PublishAsync(new JobExecutionCompletedEvent
        {
            JobId = job.Id,
            WorkerName = worker.Descriptor.Name,
            IsSuccess = false,
            Message = reason,
            CompletedAt = completedAt
        }, cancellationToken);
    }
}
