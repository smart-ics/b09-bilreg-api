using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<PluginManifestReader>();
        services.AddSingleton<WorkerPluginDiscoverer>();

        services.AddHostedService<EngineHostedService>();
        services.AddHostedService<PluginLoaderHostedService>();

        return services;
    }
}
