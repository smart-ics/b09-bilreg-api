using Taksaka.Abstractions;

namespace Taksaka.Engine.Scheduling;

public sealed class Scheduler : IScheduler
{
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
