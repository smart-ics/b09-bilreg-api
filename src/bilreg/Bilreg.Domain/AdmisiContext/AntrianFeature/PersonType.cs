using System.Text.RegularExpressions;
using Bilreg.Domain.Shared.Helpers;
using F23.StringSimilarity;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PersonType(string PersonName, DateOnly TglLahir)
{
    public static PersonType Default => new("", new DateOnly(3000, 1, 1));
    
    private static readonly HashSet<string> IgnoredWords =
    [
        "BY", "BY.", ".BY",
        "TN", "TN.", ".TN",
        "NY", "NY.", ".NY",
        "NN", "NN.",
        "BPK", "BPK.", "IBU",
        "H", "H.", "HJ", "HJ.",
        "MR", "MS", "MRS"
    ];

    public bool IsSimilar(PersonType? other)
    {
        if (other?.TglLahir != TglLahir)
            return false;
        return IsSimilarInternal(other);
    }

    public bool IsSimilarName(PersonType? other)
    {
        return IsSimilarInternal(other);
    }

    private bool IsSimilarInternal(PersonType? other)
    {
        var thisName = NormalizeName(PersonName);
        var otherName = NormalizeName(other?.PersonName);

        return JaroWinklerDistance.AreSimilar(thisName, otherName, threshold: 0.80);
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.ToUpperInvariant();
        normalized = Regex.Replace(normalized, @"[^\w\s]", " ");
        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !IgnoredWords.Contains(x));

        return string.Join(' ', tokens);
    }

}