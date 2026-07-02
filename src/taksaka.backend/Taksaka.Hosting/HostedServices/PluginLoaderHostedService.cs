using Microsoft.Extensions.Options;
using Taksaka.Abstractions;
using Taksaka.Hosting.Configuration;
using Taksaka.Hosting.Plugins;

namespace Taksaka.Hosting.HostedServices;

public sealed class PluginLoaderHostedService(
    ILogger<PluginLoaderHostedService> logger,
    IOptions<PluginLoaderOptions> options,
    WorkerPluginDiscoverer discoverer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pluginsPath = Path.GetFullPath(options.Value.PluginsPath);
        logger.LogInformation("Starting plugin discovery in {PluginsPath}", pluginsPath);
        discoverer.Discover(pluginsPath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
