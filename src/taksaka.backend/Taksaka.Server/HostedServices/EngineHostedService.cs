namespace Taksaka.Server.HostedServices;

public sealed class EngineHostedService(ILogger<EngineHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Taksaka Engine hosted service started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Taksaka Engine hosted service stopped.");
        return Task.CompletedTask;
    }
}
