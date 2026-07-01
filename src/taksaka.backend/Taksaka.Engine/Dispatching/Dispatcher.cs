using Taksaka.Abstractions;

namespace Taksaka.Engine.Dispatching;

public sealed class Dispatcher : IDispatcher
{
    public Task DispatchNextAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
