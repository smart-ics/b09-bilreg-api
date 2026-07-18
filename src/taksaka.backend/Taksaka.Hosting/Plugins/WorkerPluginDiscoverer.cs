using System.Reflection;
using System.Runtime.Loader;
using Taksaka.Abstractions;
using Taksaka.Core.Enums;

namespace Taksaka.Hosting.Plugins;

public sealed class WorkerPluginDiscoverer(
    ILogger<WorkerPluginDiscoverer> logger,
    WorkerPluginCatalog catalog,
    PluginManifestReader manifestReader)
{
    public DiscoverySummary Discover(string pluginsPath)
    {
        var summary = new DiscoverySummary();

        if (!Directory.Exists(pluginsPath))
        {
            logger.LogWarning("Plugin directory not found at {PluginsPath}", pluginsPath);
            Directory.CreateDirectory(pluginsPath);
            return summary;
        }

        var assemblies = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var assemblyPath in assemblies)
        {
            DiscoverAssembly(assemblyPath, summary);
        }

        logger.LogInformation(
            "Plugin discovery completed. Discovered={Discovered} Skipped={Skipped} Failed={Failed}",
            summary.Discovered,
            summary.Skipped,
            summary.Failed);

        return summary;
    }

    private void DiscoverAssembly(string assemblyPath, DiscoverySummary summary)
    {
        var manifestPath = PluginManifestReader.ResolveManifestPath(assemblyPath);
        var manifestResult = manifestReader.Read(manifestPath);
        if (!manifestResult.IsValid)
        {
            summary.Failed++;
            foreach (var error in manifestResult.Errors)
            {
                logger.LogError("Manifest validation failed for {AssemblyPath}: {Error}", assemblyPath, error);
            }

            return;
        }

        var manifest = manifestResult.Manifest!;

        try
        {
            var loadContext = new PluginAssemblyLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var workerTypes = assembly
                .GetTypes()
                .Where(type => typeof(IWorker).IsAssignableFrom(type)
                    && type is { IsInterface: false, IsAbstract: false })
                .ToList();

            if (workerTypes.Count != 1)
            {
                summary.Failed++;
                logger.LogError(
                    "Assembly {AssemblyPath} must contain exactly one IWorker implementation. Found {Count}.",
                    assemblyPath,
                    workerTypes.Count);
                return;
            }

            var workerType = workerTypes[0];
            if (Activator.CreateInstance(workerType) is not IWorker worker)
            {
                summary.Failed++;
                logger.LogError("Failed to create worker instance from {WorkerType}.", workerType.FullName);
                return;
            }

            if (!string.Equals(worker.Descriptor.Name, manifest.Name, StringComparison.Ordinal))
            {
                summary.Failed++;
                logger.LogError(
                    "Worker descriptor name '{DescriptorName}' does not match manifest name '{ManifestName}' in {AssemblyPath}.",
                    worker.Descriptor.Name,
                    manifest.Name,
                    assemblyPath);
                return;
            }

            if (!TryMapWorkerType(manifest.WorkerType, out var category))
            {
                summary.Failed++;
                logger.LogError("Unsupported workerType '{WorkerType}' in {AssemblyPath}.", manifest.WorkerType, assemblyPath);
                return;
            }

            if (worker.Descriptor.Category != category)
            {
                summary.Failed++;
                logger.LogError(
                    "Worker descriptor category '{DescriptorCategory}' does not match manifest workerType '{WorkerType}' in {AssemblyPath}.",
                    worker.Descriptor.Category,
                    manifest.WorkerType,
                    assemblyPath);
                return;
            }

            var registration = new RegisteredWorkerPlugin
            {
                Manifest = manifest,
                Worker = worker,
                AssemblyPath = assemblyPath,
                Category = category
            };

            if (!catalog.TryRegister(registration, out var registerError))
            {
                summary.Skipped++;
                logger.LogError("{Error} Assembly={AssemblyPath}", registerError, assemblyPath);
                return;
            }

            summary.Discovered++;
            logger.LogInformation(
                "Discovered plugin: {Name} v{Version} ({WorkerType}) from {AssemblyPath}",
                manifest.Name,
                manifest.Version,
                manifest.WorkerType,
                assemblyPath);
        }
        catch (Exception ex)
        {
            summary.Failed++;
            logger.LogError(ex, "Failed to load plugin assembly {AssemblyPath}", assemblyPath);
        }
    }

    private static bool TryMapWorkerType(string workerType, out WorkerCategory category)
    {
        if (Enum.TryParse<WorkerCategory>(workerType, ignoreCase: true, out category))
        {
            return true;
        }

        category = default;
        return false;
    }

    private sealed class PluginAssemblyLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: false)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var dependencyPath = Path.Combine(
                Path.GetDirectoryName(pluginPath) ?? string.Empty,
                $"{assemblyName.Name}.dll");

            return File.Exists(dependencyPath)
                ? LoadFromAssemblyPath(dependencyPath)
                : null;
        }
    }
}

public sealed class DiscoverySummary
{
    public int Discovered { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }
}
