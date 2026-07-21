using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.Health;

public sealed class HealthMonitor(
    IWorkerRegistry workerRegistry,
    IQueueRepository queueRepository,
    IAlertRepository alertRepository) : IHealthMonitor
{
    public async Task<HealthSnapshot> EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var workerCount = workerRegistry.RegisteredWorkerCount;
        var queueDepth = await queueRepository.GetQueueDepthAsync(cancellationToken);

        var state = HealthState.Healthy;
        var issues = new List<string>();

        if (workerCount == 0)
        {
            state = HealthState.Critical;
            issues.Add("No workers registered. Place plugin DLLs in the plugins folder.");
        }

        if (queueDepth > 1000)
        {
            state = HealthState.Warning;
            issues.Add($"Queue depth is high ({queueDepth} jobs waiting).");
        }

        if (state == HealthState.Critical)
        {
            await alertRepository.InsertAsync(new Alert
            {
                Severity = AlertSeverity.Critical,
                Message = string.Join(" ", issues)
            }, cancellationToken);
        }
        else if (state == HealthState.Warning)
        {
            await alertRepository.InsertAsync(new Alert
            {
                Severity = AlertSeverity.Warning,
                Message = string.Join(" ", issues)
            }, cancellationToken);
        }

        return new HealthSnapshot
        {
            Dimension = "Platform",
            State = state,
            EvaluatedAt = DateTimeOffset.UtcNow,
            RegisteredWorkerCount = workerCount,
            QueueDepth = queueDepth,
            Issues = issues
        };
    }
}
