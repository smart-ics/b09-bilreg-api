using Microsoft.Extensions.Options;
using Taksaka.Server.Configuration;

namespace Taksaka.Server.HostedServices;

public sealed class PluginLoaderHostedService(
    ILogger<PluginLoaderHostedService> logger,
    IOptions<PluginLoaderOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pluginsPath = Path.GetFullPath(options.Value.PluginsPath);

        if (!Directory.Exists(pluginsPath))
        {
            logger.LogWarning("Plugin directory not found at {PluginsPath}", pluginsPath);
            Directory.CreateDirectory(pluginsPath);
            return Task.CompletedTask;
        }

        var assemblies = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var assemblyPath in assemblies)
        {
            logger.LogInformation("Discovered plugin assembly: {AssemblyPath}", assemblyPath);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
