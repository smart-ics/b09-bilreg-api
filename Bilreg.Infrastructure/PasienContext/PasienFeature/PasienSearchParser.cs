using System.Globalization;
using System.Text.RegularExpressions;
using Bilreg.Application.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record PasienSearchParserResult(DateTime TglLahir, string Name);

public static class PasienSearchParser
{
    public static PasienSearchParserResult Parse(this SearchKeyword keyword)
    {
        var keywordStr = keyword.Value;
        if (string.IsNullOrWhiteSpace(keywordStr))
            return new PasienSearchParserResult(new DateTime(3000, 1, 1), "");

        var tokens = Regex.Split(keywordStr, @"[\s,]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        var tglLahir = new DateTime(3000,1,1);
        var nameParts = new List<string>();

        foreach (var token in tokens)
        {
            if (TryParseDate(token, out var result))
            {
                tglLahir = result;
                continue;
            }
            nameParts.Add(token);
        }

        return new PasienSearchParserResult(tglLahir, string.Join(" ", nameParts));
    }

    private static bool TryParseDate(string token, out DateTime tglLahir)
    {
        string[] formats = ["yyyy-MM-dd", "dd-MM-yyyy"];

        foreach (var format in formats)
        {
            if (!DateTime.TryParseExact(
                    token, format, 
                    CultureInfo.InvariantCulture, 
                    DateTimeStyles.None,
                    out tglLahir)) 
                continue;
            return true;
        }

        // Fallback general parsing (eg: 12/03/2001 or 12-03-2001)
        if (DateTime.TryParse(token, out tglLahir))
            return true;

        tglLahir = new DateTime(3000, 1, 1);
        return false;
    }
    
    public static string NormalizeToEyd(this string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var normalized = name.ToLower();

        // Common old-to-new spelling transformations
        normalized = Regex.Replace(normalized, "oe", "u");
        normalized = Regex.Replace(normalized, "tj", "c");
        normalized = Regex.Replace(normalized, "dj", "j");
        normalized = Regex.Replace(normalized, "j", "y"); // Optional: "j" → "y" (e.g., Jogja → Yogya)
        normalized = Regex.Replace(normalized, "nj", "ny");
        normalized = Regex.Replace(normalized, "sj", "sy");
        normalized = Regex.Replace(normalized, "ch", "kh");
        normalized = Regex.Replace(normalized, "dh", "d");
        

        return normalized;
    }    
}

