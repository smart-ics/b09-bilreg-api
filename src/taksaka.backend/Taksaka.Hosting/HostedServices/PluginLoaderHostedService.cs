using Microsoft.Extensions.Options;
using Taksaka.Hosting.Configuration;
using Taksaka.Hosting.Plugins;

namespace Taksaka.Hosting.HostedServices;

public sealed class PluginLoaderHostedService(
    ILogger<PluginLoaderHostedService> logger,
    IOptions<PluginLoaderOptions> options,
    WorkerPluginDiscoverer discoverer,
    WorkerPluginCatalog catalog) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pluginsPath = Path.GetFullPath(options.Value.PluginsPath);
        logger.LogInformation("Loading plugins from {PluginsPath}...", pluginsPath);

        var summary = discoverer.Discover(pluginsPath);
        var totalFound = summary.Discovered + summary.Skipped + summary.Failed;

        logger.LogInformation("Found {Count} plugin assemblies", totalFound);

        foreach (var registration in catalog.Registrations)
        {
            logger.LogInformation(
                "{WorkerName} loaded (v{Version}, {WorkerType})",
                registration.Manifest.Name,
                registration.Manifest.Version,
                registration.Manifest.WorkerType);
        }

        if (summary.Failed > 0)
        {
            logger.LogWarning(
                "Plugin loading completed with {Failed} failure(s). See errors above for details.",
                summary.Failed);
        }

        if (summary.Skipped > 0)
        {
            logger.LogWarning(
                "Plugin loading skipped {Skipped} duplicate(s).",
                summary.Skipped);
        }

        logger.LogInformation("Registered {Count} workers", catalog.RegisteredWorkerCount);

        if (catalog.RegisteredWorkerCount == 0)
        {
            logger.LogWarning(
                "No workers registered. Place plugin DLLs and manifests in {PluginsPath}. " +
                "See docs/taksaka/06-plugin-development-guide.md for packaging instructions.",
                pluginsPath);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
