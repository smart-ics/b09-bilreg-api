namespace Taksaka.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : class;
}
