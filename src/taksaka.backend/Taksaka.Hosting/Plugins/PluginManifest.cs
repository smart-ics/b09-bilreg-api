using System.Text.Json;

namespace Taksaka.Hosting.Plugins;

public sealed class PluginManifest
{
    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string WorkerType { get; init; } = string.Empty;

    public JsonElement Configuration { get; init; }
}
