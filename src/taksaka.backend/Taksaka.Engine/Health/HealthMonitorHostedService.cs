using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions;
using Taksaka.Core.Enums;
using Taksaka.Engine.Configuration;

namespace Taksaka.Engine.Health;

public sealed class HealthMonitorHostedService(
    IHealthMonitor healthMonitor,
    IOptions<EngineOptions> options,
    ILogger<HealthMonitorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, options.Value.HealthMonitorIntervalSeconds));
        logger.LogInformation("Health monitor started (interval={IntervalSeconds}s)", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snapshot = await healthMonitor.EvaluateAsync(stoppingToken);
                var logLevel = snapshot.State switch
                {
                    HealthState.Healthy => LogLevel.Debug,
                    HealthState.Warning => LogLevel.Warning,
                    _ => LogLevel.Error
                };

                logger.Log(
                    logLevel,
                    "Health check: state={State} workers={WorkerCount} queueDepth={QueueDepth} issues={Issues}",
                    snapshot.State,
                    snapshot.RegisteredWorkerCount,
                    snapshot.QueueDepth,
                    snapshot.Issues.Count > 0 ? string.Join("; ", snapshot.Issues) : "none");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health monitor tick failed.");
            }
        }

        logger.LogInformation("Health monitor stopped.");
    }
}
