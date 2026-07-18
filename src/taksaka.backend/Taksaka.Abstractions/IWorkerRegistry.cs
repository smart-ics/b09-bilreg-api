namespace Taksaka.Abstractions;

public interface IWorkerRegistry
{
    bool TryGetWorker(string name, out IWorker? worker);

    IReadOnlyCollection<string> RegisteredWorkerNames { get; }

    int RegisteredWorkerCount { get; }
}
