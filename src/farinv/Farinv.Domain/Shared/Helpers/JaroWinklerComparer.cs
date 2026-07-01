using System.Runtime.CompilerServices;

namespace Farinv.Domain.Shared.Helpers;

public static class JaroWinklerDistance
{
    private const double DEFAULT_SCALING_FACTOR = 0.1;
    private const int PREFIX_LENGTH = 4;

    /// <summary>
    /// Calculates Jaro-Winkler similarity between two person names (0.0 to 1.0, where 1.0 is exact match)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Calculate(string s1, string s2, double scalingFactor = DEFAULT_SCALING_FACTOR)
    {
        if (s1 == null || s2 == null)
            return 0.0;

        int len1 = s1.Length;
        int len2 = s2.Length;

        if (len1 == 0 && len2 == 0)
            return 1.0;
        if (len1 == 0 || len2 == 0)
            return 0.0;

        // Quick check for exact match
        if (s1 == s2)
            return 1.0;

        // Calculate Jaro distance
        double jaroDistance = CalculateJaro(s1, s2, len1, len2);

        if (jaroDistance < 0.7)
            return jaroDistance;

        // Calculate common prefix for Winkler bonus (up to 4 chars)
        int prefixLen = 0;
        int maxPrefix = Math.Min(Math.Min(len1, len2), PREFIX_LENGTH);
        
        for (int i = 0; i < maxPrefix; i++)
        {
            if (s1[i] == s2[i])
                prefixLen++;
            else
                break;
        }

        // Apply Winkler modification
        return jaroDistance + (prefixLen * scalingFactor * (1.0 - jaroDistance));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CalculateJaro(string s1, string s2, int len1, int len2)
    {
        // Match window: max(len1, len2) / 2 - 1
        int matchWindow = (Math.Max(len1, len2) / 2) - 1;
        if (matchWindow < 1)
            matchWindow = 1;

        Span<bool> s1Matches = stackalloc bool[len1];
        Span<bool> s2Matches = stackalloc bool[len2];

        int matches = 0;
        int transpositions = 0;

        // Find matches
        for (int i = 0; i < len1; i++)
        {
            int start = Math.Max(0, i - matchWindow);
            int end = Math.Min(i + matchWindow + 1, len2);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                    continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        // Count transpositions
        int k = 0;
        for (int i = 0; i < len1; i++)
        {
            if (!s1Matches[i])
                continue;

            while (!s2Matches[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        return (matches / (double)len1 +
                matches / (double)len2 +
                (matches - transpositions / 2.0) / matches) / 3.0;
    }

    /// <summary>
    /// Case-insensitive comparison for person names
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateIgnoreCase(string s1, string s2, double scalingFactor = DEFAULT_SCALING_FACTOR)
    {
        if (s1 == null || s2 == null)
            return 0.0;

        return Calculate(s1.ToUpperInvariant(), s2.ToUpperInvariant(), scalingFactor);
    }

    /// <summary>
    /// Determines if two names are similar based on a threshold
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreSimilar(string s1, string s2, double threshold = 0.9, bool ignoreCase = true)
    {
        double similarity = ignoreCase 
            ? CalculateIgnoreCase(s1, s2) 
            : Calculate(s1, s2);
        
        return similarity >= threshold;
    }
}