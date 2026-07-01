using Taksaka.Abstractions;

namespace Taksaka.Engine.Execution;

public sealed class EventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : class =>
        Task.CompletedTask;
}
