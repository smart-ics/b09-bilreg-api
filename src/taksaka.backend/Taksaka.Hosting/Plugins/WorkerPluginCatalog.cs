using Taksaka.Abstractions;
using Taksaka.Core.Enums;

namespace Taksaka.Hosting.Plugins;

public sealed class WorkerPluginCatalog
{
    private readonly Dictionary<string, RegisteredWorkerPlugin> _workers = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IWorker> Workers =>
        _workers.ToDictionary(pair => pair.Key, pair => pair.Value.Worker, StringComparer.Ordinal);

    public IReadOnlyCollection<RegisteredWorkerPlugin> Registrations => _workers.Values;

    public bool TryRegister(RegisteredWorkerPlugin registration, out string? error)
    {
        if (_workers.ContainsKey(registration.Manifest.Name))
        {
            error = $"Duplicate worker name '{registration.Manifest.Name}'.";
            return false;
        }

        _workers[registration.Manifest.Name] = registration;
        error = null;
        return true;
    }
}

public sealed class RegisteredWorkerPlugin
{
    public required PluginManifest Manifest { get; init; }

    public required IWorker Worker { get; init; }

    public required string AssemblyPath { get; init; }

    public WorkerCategory Category { get; init; }
}
