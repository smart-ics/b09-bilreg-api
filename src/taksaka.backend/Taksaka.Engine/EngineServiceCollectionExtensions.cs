using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
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
    public static IServiceCollection AddTaksakaEngine(this IServiceCollection services)
    {
        services.AddSingleton<IScheduler, Scheduler>();
        services.AddSingleton<IQueue, QueueManager>();
        services.AddSingleton<IDispatcher, Dispatcher>();
        services.AddSingleton<IResourceManager, ResourceManager>();
        services.AddSingleton<IRetryManager, RetryManager>();
        services.AddSingleton<IDeadLetterManager, DeadLetterManager>();
        services.AddSingleton<IHealthMonitor, HealthMonitor>();
        services.AddSingleton<IEventPublisher, EventPublisher>();

        return services;
    }
}
