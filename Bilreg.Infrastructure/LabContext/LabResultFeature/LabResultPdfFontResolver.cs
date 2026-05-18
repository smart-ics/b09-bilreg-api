using PdfSharp.Fonts;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

/// <summary>
/// Resolves Arial from the OS fonts folder (Windows) for on-demand PDF rendering.
/// </summary>
internal sealed class LabResultPdfFontResolver : IFontResolver
{
    private static readonly Dictionary<string, byte[]> Cache = new(StringComparer.OrdinalIgnoreCase);

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        _ = familyName;
        var suffix = isBold ? (isItalic ? "bi" : "b") : (isItalic ? "i" : "r");
        return new FontResolverInfo($"Arial#{suffix}");
    }

    public byte[]? GetFont(string faceName)
    {
        if (Cache.TryGetValue(faceName, out var cached))
            return cached;

        var file = faceName switch
        {
            var f when f.StartsWith("Arial#b", StringComparison.OrdinalIgnoreCase) => "arialbd.ttf",
            var f when f.StartsWith("Arial#i", StringComparison.OrdinalIgnoreCase) => "ariali.ttf",
            _ => "arial.ttf"
        };

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), file);
        if (!File.Exists(path))
            return null;

        var bytes = File.ReadAllBytes(path);
        Cache[faceName] = bytes;
        return bytes;
    }

    public static void EnsureRegistered()
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new LabResultPdfFontResolver();
    }
}
