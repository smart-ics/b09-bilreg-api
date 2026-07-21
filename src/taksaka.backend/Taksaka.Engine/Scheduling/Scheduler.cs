using Cronos;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.Scheduling;

public sealed class Scheduler(
    IScheduleRepository scheduleRepository,
    IJobRepository jobRepository,
    IQueueRepository queueRepository) : IScheduler
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _runLoopTask;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runLoopTask = RunLoopAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource is null)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();

        if (_runLoopTask is not null)
        {
            try
            {
                await _runLoopTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the scheduler loop.
            }
        }

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        _runLoopTask = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await ProcessSchedulesAsync(cancellationToken);
            await ProcessRetriesAsync(cancellationToken);
        }
    }

    private async Task ProcessSchedulesAsync(CancellationToken cancellationToken)
    {
        var schedules = await scheduleRepository.GetEnabledAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var schedule in schedules)
        {
            if (schedule.NextRunAt > now)
            {
                continue;
            }

            var job = new Job
            {
                Payload = schedule.PayloadTemplate,
                WorkerName = schedule.WorkerName,
                Priority = JobPriority.Normal,
                Status = JobStatus.Created
            };

            await jobRepository.InsertAsync(job, cancellationToken: cancellationToken);
            await queueRepository.EnqueueAsync(job, cancellationToken: cancellationToken);
            await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);

            var nextRunAt = GetNextOccurrence(schedule.CronExpression, now) ?? now.Add(TickInterval);
            await scheduleRepository.UpdateRunTimesAsync(schedule.Id, now, nextRunAt, cancellationToken);
        }
    }

    private async Task ProcessRetriesAsync(CancellationToken cancellationToken)
    {
        var jobs = await jobRepository.GetRetryReadyAsync(DateTimeOffset.UtcNow, cancellationToken);

        foreach (var job in jobs)
        {
            await queueRepository.EnqueueAsync(job, cancellationToken: cancellationToken);
            await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
        }
    }

    private static DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset from)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            return expression.GetNextOccurrence(from, TimeZoneInfo.Utc);
        }
        catch (CronFormatException)
        {
            var expression = CronExpression.Parse(cronExpression);
            return expression.GetNextOccurrence(from, TimeZoneInfo.Utc);
        }
    }
}
