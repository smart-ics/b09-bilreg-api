using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions;
using Taksaka.Engine.Configuration;

namespace Taksaka.Engine.Dispatching;

public sealed class DispatcherHostedService(
    IDispatcher dispatcher,
    IOptions<EngineOptions> options,
    ILogger<DispatcherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.DispatchPollIntervalSeconds));
        logger.LogInformation("Queue polling started (interval={IntervalSeconds}s)", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await dispatcher.DispatchNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dispatcher polling tick failed.");
            }
        }

        logger.LogInformation("Queue polling stopped.");
    }
}
