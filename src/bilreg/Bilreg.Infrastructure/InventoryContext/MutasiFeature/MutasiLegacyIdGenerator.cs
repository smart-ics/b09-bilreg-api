using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

internal static class MutasiLegacyIdGenerator
{
    private const string Base36Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly object IdLock = new();
    private static readonly Dictionary<string, string> LastIds = new(StringComparer.Ordinal);

    public static string NextId(string prefix)
    {
        lock (IdLock)
        {
            var candidate = NunaId.NewLegacyCompact(prefix);
            if (candidate.Length > 10)
                candidate = candidate[..10];

            if (LastIds.TryGetValue(prefix, out var last)
                && string.Compare(candidate, last, StringComparison.Ordinal) <= 0)
            {
                candidate = IncrementBase36Suffix(last, prefix);
            }

            LastIds[prefix] = candidate;
            return candidate;
        }
    }

    private static string IncrementBase36Suffix(string id, string prefix)
    {
        var chars = id.ToCharArray();
        for (var i = chars.Length - 1; i >= prefix.Length; i--)
        {
            var idx = Base36Alphabet.IndexOf(chars[i]);
            if (idx < 0)
                continue;

            if (idx < Base36Alphabet.Length - 1)
            {
                chars[i] = Base36Alphabet[idx + 1];
                return new string(chars);
            }

            chars[i] = Base36Alphabet[0];
        }

        return new string(chars);
    }
}
