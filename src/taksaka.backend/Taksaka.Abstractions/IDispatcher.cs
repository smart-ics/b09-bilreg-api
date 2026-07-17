namespace Taksaka.Abstractions;

public interface IDispatcher
{
    Task DispatchNextAsync(CancellationToken cancellationToken = default);
}
