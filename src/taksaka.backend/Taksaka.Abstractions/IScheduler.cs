namespace Taksaka.Abstractions;

public interface IScheduler
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
