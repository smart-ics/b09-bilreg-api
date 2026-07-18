using Microsoft.Extensions.Logging;
using Taksaka.Abstractions;

namespace Taksaka.Engine.Execution;

public sealed class EventPublisher(ILogger<EventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogDebug("Domain event published: {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }
}
