using Taksaka.Abstractions;

namespace Taksaka.Hosting.HostedServices;

public sealed class EngineHostedService(
    IScheduler scheduler,
    ILogger<EngineHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await scheduler.StartAsync(cancellationToken);
        logger.LogInformation("Scheduler started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await scheduler.StopAsync(cancellationToken);
        logger.LogInformation("Scheduler stopped.");
    }
}
