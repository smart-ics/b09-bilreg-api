using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
using Taksaka.Hosting.Configuration;
using Taksaka.Hosting.HostedServices;
using Taksaka.Hosting.Plugins;

namespace Taksaka.Hosting.DependencyInjection;

public static class HostingServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaHosting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PluginLoaderOptions>(configuration.GetSection(PluginLoaderOptions.SectionName));

        services.AddSingleton<WorkerPluginCatalog>();
        services.AddSingleton<IWorkerRegistry>(sp => sp.GetRequiredService<WorkerPluginCatalog>());
        services.AddSingleton<PluginManifestReader>();
        services.AddSingleton<WorkerPluginDiscoverer>();

        services.AddHostedService<PluginLoaderHostedService>();
        services.AddHostedService<EngineHostedService>();

        return services;
    }
}
