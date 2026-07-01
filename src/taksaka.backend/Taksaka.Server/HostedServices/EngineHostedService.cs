using Taksaka.Abstractions;

namespace Taksaka.Server.HostedServices;

public sealed class EngineHostedService(
    IScheduler scheduler,
    ILogger<EngineHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await scheduler.StartAsync(cancellationToken);
        logger.LogInformation("Taksaka Engine hosted service started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await scheduler.StopAsync(cancellationToken);
        logger.LogInformation("Taksaka Engine hosted service stopped.");
    }
}
