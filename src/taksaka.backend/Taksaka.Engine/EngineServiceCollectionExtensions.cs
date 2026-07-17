using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
using Taksaka.Engine.Configuration;
using Taksaka.Engine.DeadLetter;
using Taksaka.Engine.Dispatching;
using Taksaka.Engine.Execution;
using Taksaka.Engine.Health;
using Taksaka.Engine.Queue;
using Taksaka.Engine.Resources;
using Taksaka.Engine.Retry;
using Taksaka.Engine.Scheduling;

namespace Taksaka.Engine;

public static class EngineServiceCollectionExtensions
{
    public static IServiceCollection AddTaksakaEngine(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<EngineOptions>(configuration.GetSection(EngineOptions.SectionName));
        }
        else
        {
            services.AddOptions<EngineOptions>();
        }

        services.AddSingleton<IScheduler, Scheduler>();
        services.AddSingleton<IQueue, QueueManager>();
        services.AddSingleton<IDispatcher, Dispatcher>();
        services.AddSingleton<IResourceManager, ResourceManager>();
        services.AddSingleton<IRetryManager, RetryManager>();
        services.AddSingleton<IDeadLetterManager, DeadLetterManager>();
        services.AddSingleton<IHealthMonitor, HealthMonitor>();
        services.AddSingleton<IEventPublisher, EventPublisher>();

        services.AddHostedService<DispatcherHostedService>();
        services.AddHostedService<HealthMonitorHostedService>();

        return services;
    }
}
