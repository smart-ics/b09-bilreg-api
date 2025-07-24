using FluentAssertions;
using Xunit;
using System.Diagnostics.CodeAnalysis;

namespace Bilreg.Infrastructure.Helpers;

[SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
public class JaroWinklerHelper 
{
    private const double DEFAULT_THRESHOLD = 0.7;
    private const int THREE = 3;
    private const double JW_COEF = 0.1;
    private double Threshold { get; }
        
    public JaroWinklerHelper()
    {
        Threshold = DEFAULT_THRESHOLD;
    }
    public JaroWinklerHelper(double threshold)
    {
        Threshold = threshold;
    }

    public double Similarity(string s1, string s2)
        => Similarity(s1.AsSpan(), s2.AsSpan());
        
    public double Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
        where T : IEquatable<T>
    {
        if (s1 == null)
            throw new ArgumentNullException(nameof(s1));    

        if (s2 == null)
            throw new ArgumentNullException(nameof(s2));

        if (s1.SequenceEqual(s2))
            return 1f;

        var mtp = Matches(s1, s2);
        float m = mtp[0];
        if (m == 0)
            return 0f;

        double j = ((m / s1.Length + m / s2.Length + (m - mtp[1]) / m))
                   / THREE;
        var jw = j;

        if (j > Threshold)
        {
            jw = j + Math.Min(JW_COEF, 1.0 / mtp[THREE]) * mtp[2] * (1 - j);
        }
        return jw;
    }

    public double Distance(string s1, string s2)
        => 1.0 - Similarity(s1, s2);
        
    public double Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
        where T : IEquatable<T>
        => 1.0 - Similarity(s1, s2);

    private static int[] Matches<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
        where T : IEquatable<T>
    {
        ReadOnlySpan<T> max, min;
        if (s1.Length > s2.Length)
        {
            max = s1;
            min = s2;
        }
        else
        {
            max = s2;
            min = s1;
        }
        var range = Math.Max(max.Length / 2 - 1, 0);

        var matchIndexes = Enumerable.Repeat(-1, min.Length).ToArray();

        var matchFlags = new bool[max.Length];
        var matches = 0;
        for (var mi = 0; mi < min.Length; mi++)
        {
            var c1 = min[mi];
            for (int xi = Math.Max(mi - range, 0),
                 xn = Math.Min(mi + range + 1, max.Length); xi < xn; xi++)
            {
                if (matchFlags[xi] || !c1.Equals(max[xi])) 
                    continue;
                
                matchIndexes[mi] = xi;
                matchFlags[xi] = true;
                matches++;
                break;
            }
        }
        
        var ms1 = new T[matches];
        var ms2 = new T[matches];
        for (int i = 0, si = 0; i < min.Length; i++)
        {
            if (matchIndexes[i] == -1) 
                continue;
            ms1[si] = min[i];
            si++;
        }
        
        for (int i = 0, si = 0; i < max.Length; i++)
        {
            if (!matchFlags[i]) 
                continue;
            ms2[si] = max[i];
            si++;
        }
        
        var transpositions = 0;
        for (var mi = 0; mi < ms1.Length; mi++)
        {
            if (!ms1[mi].Equals(ms2[mi]))
            {
                transpositions++;
            }
        }
        
        var prefix = 0;
        for (var mi = 0; mi < min.Length; mi++)
        {
            if (s1[mi].Equals(s2[mi]))
                prefix++;
            else
                break;
        }
        return [matches, transpositions / 2, prefix, max.Length];
    }
}

