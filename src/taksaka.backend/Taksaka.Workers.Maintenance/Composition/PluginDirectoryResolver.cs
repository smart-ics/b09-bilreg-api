using System.Reflection;

namespace Taksaka.Workers.Maintenance.Composition;

internal static class PluginDirectoryResolver
{
    public static string Resolve()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrWhiteSpace(assemblyLocation))
        {
            return AppContext.BaseDirectory;
        }

        return Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
    }
}
