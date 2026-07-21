using System.Text.Json;
using System.Text.RegularExpressions;

namespace Taksaka.Hosting.Plugins;

public sealed partial class PluginManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] AllowedWorkerTypes =
    [
        "Projection",
        "Integration",
        "Business",
        "Maintenance"
    ];

    public PluginManifestValidationResult Read(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return PluginManifestValidationResult.Failure($"Manifest not found: {manifestPath}");
        }

        PluginManifest manifest;
        try
        {
            using var stream = File.OpenRead(manifestPath);
            manifest = JsonSerializer.Deserialize<PluginManifest>(stream, JsonOptions)
                ?? throw new InvalidOperationException("Manifest deserialized to null.");
        }
        catch (Exception ex)
        {
            return PluginManifestValidationResult.Failure($"Failed to parse manifest: {ex.Message}");
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("Manifest 'name' is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || !VersionPattern().IsMatch(manifest.Version))
        {
            errors.Add("Manifest 'version' must match semver pattern (e.g. 1.0.0).");
        }

        if (string.IsNullOrWhiteSpace(manifest.Description))
        {
            errors.Add("Manifest 'description' is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.WorkerType)
            || !AllowedWorkerTypes.Contains(manifest.WorkerType, StringComparer.Ordinal))
        {
            errors.Add("Manifest 'workerType' must be one of: Projection, Integration, Business, Maintenance.");
        }

        if (manifest.Configuration.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            errors.Add("Manifest 'configuration' is required.");
        }

        return errors.Count == 0
            ? PluginManifestValidationResult.Success(manifest)
            : PluginManifestValidationResult.Failure(errors.ToArray());
    }

    public static string ResolveManifestPath(string assemblyPath)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        return Path.Combine(directory, $"{assemblyName}.plugin.json");
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+")]
    private static partial Regex VersionPattern();
}
