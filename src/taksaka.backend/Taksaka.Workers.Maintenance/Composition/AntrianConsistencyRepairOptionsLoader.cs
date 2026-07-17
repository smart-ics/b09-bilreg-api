using System.Text.Json;
using Taksaka.Workers.Maintenance.Configuration;

namespace Taksaka.Workers.Maintenance.Composition;

internal static class AntrianConsistencyRepairOptionsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AntrianConsistencyRepairOptions Load(string pluginDirectory)
    {
        var manifestPath = Path.Combine(
            pluginDirectory,
            $"{typeof(AntrianConsistencyRepairOptionsLoader).Assembly.GetName().Name}.plugin.json");

        if (!File.Exists(manifestPath))
        {
            return new AntrianConsistencyRepairOptions();
        }

        using var stream = File.OpenRead(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifestDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Invalid plugin manifest: {manifestPath}");

        return new AntrianConsistencyRepairOptions
        {
            BatchSize = manifest.Configuration.BatchSize ?? 50,
            WorkerEnabled = manifest.Configuration.WorkerEnabled ?? true,
            ExecutionTimeout = ParseTimeout(manifest.Configuration.ExecutionTimeout),
            ConnectionString = manifest.Configuration.ConnectionString ?? string.Empty
        };
    }

    private static TimeSpan ParseTimeout(string? value) =>
        TimeSpan.TryParse(value, out var timeout) ? timeout : TimeSpan.FromMinutes(10);

    private sealed class PluginManifestDocument
    {
        public ConfigurationSection Configuration { get; init; } = new();
    }

    private sealed class ConfigurationSection
    {
        public int? BatchSize { get; init; }

        public bool? WorkerEnabled { get; init; }

        public string? ExecutionTimeout { get; init; }

        public string? ConnectionString { get; init; }
    }
}
