namespace Taksaka.Hosting.Configuration;

public sealed class PluginLoaderOptions
{
    public const string SectionName = "PluginLoader";

    public string PluginsPath { get; set; } = "./plugins";
}
