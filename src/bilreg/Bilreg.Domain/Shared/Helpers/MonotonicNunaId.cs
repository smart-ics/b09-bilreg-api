using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.Shared.Helpers;

/// <summary>
/// Thread-safe monotonic Base36 ID generator wrapping <see cref="NunaId"/> without <see cref="Thread.Sleep(int)"/>.
/// Guarantees strictly increasing <see cref="StringComparer.Ordinal"/> IDs per prefix within the process.
/// </summary>
public static class MonotonicNunaId
{
    private const string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int StandardPrefixLength = 3;
    private const int StandardSuffixLength = 9;
    private const int CompactPrefixLength = 2;
    private const int CompactSuffixLength = 8;

    private static readonly long StandardMaxExclusive = Pow36(StandardSuffixLength);
    private static readonly long CompactMaxExclusive = Pow36(CompactSuffixLength);

    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, long> LastValues = new(StringComparer.Ordinal);

    /// <summary>
    /// Generates a 12-character monotonic ID (3-character prefix + 9-character Base36 suffix).
    /// </summary>
    public static string New(string prefix, string? minExclusiveId = null)
    {
        Guard.Against.NullOrWhiteSpace(prefix);
        if (prefix.Length != StandardPrefixLength)
            throw new ArgumentException($"Prefix must be {StandardPrefixLength} characters.", nameof(prefix));

        return NextId(
            prefix,
            StandardPrefixLength,
            StandardSuffixLength,
            StandardMaxExclusive,
            static p => NunaId.New(p),
            "NEW",
            minExclusiveId);
    }

    /// <summary>
    /// Generates a 10-character monotonic legacy compact ID (2-character prefix + 8-character Base36 suffix).
    /// </summary>
    public static string NewLegacyCompact(string prefix, string? minExclusiveId = null)
    {
        Guard.Against.NullOrWhiteSpace(prefix);
        if (prefix.Length != CompactPrefixLength)
            throw new ArgumentException($"Prefix must be {CompactPrefixLength} characters.", nameof(prefix));

        return NextId(
            prefix,
            CompactPrefixLength,
            CompactSuffixLength,
            CompactMaxExclusive,
            static p => NunaId.NewLegacyCompact(p),
            "COMPACT",
            minExclusiveId);
    }

    private static string NextId(
        string prefix,
        int prefixLength,
        int suffixLength,
        long maxExclusive,
        Func<string, string> rawGenerator,
        string bucket,
        string? minExclusiveId)
    {
        var expectedLength = prefixLength + suffixLength;

        lock (SyncRoot)
        {
            var candidate = rawGenerator(prefix);
            if (candidate.Length != expectedLength)
                throw new InvalidOperationException(
                    $"Expected NunaId length {expectedLength} for prefix '{prefix}', got {candidate.Length}.");

            var normalizedPrefix = candidate[..prefixLength];
            var candidateVal = ParseBase36(candidate.AsSpan(prefixLength, suffixLength));

            long minVal = -1;
            if (!string.IsNullOrWhiteSpace(minExclusiveId) &&
                minExclusiveId.Length == expectedLength &&
                minExclusiveId.AsSpan(0, prefixLength).Equals(normalizedPrefix.AsSpan(), StringComparison.Ordinal) &&
                TryParseBase36(minExclusiveId.AsSpan(prefixLength, suffixLength), out var parsedMin))
            {
                minVal = parsedMin;
            }

            var key = $"{bucket}:{normalizedPrefix}";
            var lastVal = LastValues.TryGetValue(key, out var existing) ? existing : -1L;
            var floor = Math.Max(lastVal, minVal);
            var nextVal = floor >= 0
                ? Math.Max(candidateVal, floor + 1)
                : candidateVal;

            if (nextVal >= maxExclusive)
                throw new InvalidOperationException($"MonotonicNunaId overflow for prefix '{normalizedPrefix}'.");

            LastValues[key] = nextVal;
            return FormatId(normalizedPrefix, nextVal, prefixLength, suffixLength);
        }
    }

    private static string FormatId(string prefix, long value, int prefixLength, int suffixLength)
    {
        return string.Create(prefixLength + suffixLength, (prefix, value, prefixLength, suffixLength), static (span, state) =>
        {
            state.prefix.AsSpan().CopyTo(span);
            var remaining = state.value;
            for (var i = state.prefixLength + state.suffixLength - 1; i >= state.prefixLength; i--)
            {
                span[i] = Base36Chars[(int)(remaining % 36)];
                remaining /= 36;
            }
        });
    }

    private static long ParseBase36(ReadOnlySpan<char> chars)
    {
        if (!TryParseBase36(chars, out var value))
            throw new FormatException($"Invalid Base36 value '{chars.ToString()}'.");
        return value;
    }

    private static bool TryParseBase36(ReadOnlySpan<char> chars, out long value)
    {
        value = 0;
        for (var i = 0; i < chars.Length; i++)
        {
            var c = char.ToUpperInvariant(chars[i]);
            int digit;
            if (c is >= '0' and <= '9')
                digit = c - '0';
            else if (c is >= 'A' and <= 'Z')
                digit = c - 'A' + 10;
            else
                return false;

            value = value * 36 + digit;
        }

        return true;
    }

    private static long Pow36(int exponent)
    {
        long result = 1;
        for (var i = 0; i < exponent; i++)
            result *= 36;
        return result;
    }
}
