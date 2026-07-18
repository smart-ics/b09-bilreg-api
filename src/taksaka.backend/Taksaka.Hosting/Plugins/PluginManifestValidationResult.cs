namespace Taksaka.Hosting.Plugins;

public sealed class PluginManifestValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = [];

    public PluginManifest? Manifest { get; init; }

    public static PluginManifestValidationResult Success(PluginManifest manifest) =>
        new() { Manifest = manifest };

    public static PluginManifestValidationResult Failure(params string[] errors)
    {
        var result = new PluginManifestValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }
}
