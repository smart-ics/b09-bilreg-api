using System.Text.RegularExpressions;

namespace Bilreg.Domain.PaymentContext.RegOutFeature;

public record RegFinder(
    string RegId,
    string PasienId,
    string PasienName,
    string BookingId,
    Dictionary<string, string[]> StringVariants)
{
    public static RegFinder Create(string regId, string pasienId, string pasienName, string bookingId)
    {
        var variants = GenerateStringVariants(pasienId, pasienName);
        return new RegFinder(regId, pasienId, pasienName, bookingId, variants);
    }

    private static Dictionary<string, string[]> GenerateStringVariants(string pasienId, string pasienName)
    {
        var variants = new Dictionary<string, string[]>();

        // Variants for PasienName
        variants["PasienName"] = GenerateNameVariants(pasienName);

        // Variants for PasienId
        variants["PasienId"] = GenerateIdVariants(pasienId);

        return variants;
    }

    private static string[] GenerateNameVariants(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<string>();

        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Original name
        variants.Add(name.Trim());

        // Without spaces
        variants.Add(name.Replace(" ", ""));

        // Without spaces and lowercase
        variants.Add(name.Replace(" ", "").ToLower());

        // Without spaces and uppercase
        variants.Add(name.Replace(" ", "").ToUpper());

        // Without accents/diacritics
        var withoutAccents = RemoveAccents(name.Replace(" ", ""));
        variants.Add(withoutAccents);
        variants.Add(withoutAccents.ToLower());
        variants.Add(withoutAccents.ToUpper());

        // Split names and combinations
        var nameParts = name.Split(new char[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length > 1)
        {
            // First name only
            variants.Add(nameParts[0]);

            // Last name only
            variants.Add(nameParts[^1]);

            // First and last name combination
            if (nameParts.Length >= 2)
            {
                variants.Add($"{nameParts[0]} {nameParts[^1]}");
                variants.Add($"{nameParts[^1]}, {nameParts[0]}");
            }

            // All possible combinations of name parts
            for (int i = 0; i < nameParts.Length; i++)
            {
                for (int j = i + 1; j <= nameParts.Length; j++)
                {
                    var combination = string.Join(" ", nameParts[i..j]).Trim();
                    if (!string.IsNullOrEmpty(combination))
                    {
                        variants.Add(combination);
                    }
                }
            }
        }

        // With common prefixes/suffixes removed
        var cleanedName = CleanName(name);
        if (!string.Equals(cleanedName, name, StringComparison.OrdinalIgnoreCase))
        {
            variants.Add(cleanedName);
            variants.Add(cleanedName.Replace(" ", ""));
        }

        return variants.ToArray();
    }

    private static string[] GenerateIdVariants(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Array.Empty<string>();

        var variants = new HashSet<string>();

        // Original ID
        variants.Add(id.Trim());

        // Without leading zeros
        variants.Add(id.TrimStart('0'));

        // Without non-alphanumeric characters
        variants.Add(Regex.Replace(id, @"[^0-9A-Za-z]", ""));

        // With different separators removed
        variants.Add(id.Replace("-", "").Replace(".", "").Replace("/", "").Replace(" ", ""));

        return variants.ToArray();
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var result = new System.Text.StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                result.Append(c);
            }
        }

        return result.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static string CleanName(string name)
    {
        // Remove common titles/prefixes
        var cleanName = Regex.Replace(name, @"\b(Dr|Dr\.|Prof|Prof\.|Ir|Ir\.|H|H\.|Hj|Hj\.|Ust|Ust\.|Bpk|Bpk\.|Ibu|Ibu\.|Sdr|Sdr\.|Mrs|Mr|Ms)\b", "", RegexOptions.IgnoreCase);

        // Remove common suffixes
        cleanName = Regex.Replace(cleanName, @"\b(SH|SE|MH|AK|SKM|AMK|SIP|SAR|S.Sos|S.Hum|M.M|S.E.I|M.B.A|M.T|S.T)\b", "", RegexOptions.IgnoreCase);

        // Remove extra whitespace
        cleanName = Regex.Replace(cleanName.Trim(), @"\s+", " ");

        return cleanName;
    }

    public bool MatchesKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        // Check exact matches
        if (IsMatch(RegId, keyword) ||
            IsMatch(PasienId, keyword) ||
            IsMatch(PasienName, keyword) ||
            IsMatch(BookingId, keyword))
        {
            return true;
        }

        // Check variants
        foreach (var variantGroup in StringVariants.Values)
        {
            foreach (var variant in variantGroup)
            {
                if (IsMatch(variant, keyword))
                    return true;
            }
        }

        return false;
    }

    private static bool IsMatch(string source, string keyword)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(keyword))
            return false;

        // Exact match
        if (string.Equals(source, keyword, StringComparison.OrdinalIgnoreCase))
            return true;

        // Contains match
        if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            return true;

        // StartsWith match
        if (source.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return true;

        // EndsWith match
        if (source.EndsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return true;

        // Word boundary match (keyword appears as whole word in source)
        var pattern = $@"\b{Regex.Escape(keyword)}\b";
        if (Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase))
            return true;

        return false;
    }

    public double CalculateSimilarityScore(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return 0;

        var scores = new List<double>();

        // Score for each field
        scores.Add(CalculateFieldSimilarity(RegId, keyword));
        scores.Add(CalculateFieldSimilarity(PasienId, keyword));
        scores.Add(CalculateFieldSimilarity(PasienName, keyword));
        scores.Add(CalculateFieldSimilarity(BookingId, keyword));

        // Score for variants
        foreach (var variantGroup in StringVariants.Values)
        {
            foreach (var variant in variantGroup)
            {
                scores.Add(CalculateFieldSimilarity(variant, keyword));
            }
        }

        return scores.Max();
    }

    private static double CalculateFieldSimilarity(string source, string keyword)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(keyword))
            return 0;

        // Exact match - perfect score
        if (string.Equals(source, keyword, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Case-insensitive contains - high score
        if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            return 0.8;

        // StartsWith - medium-high score
        if (source.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return 0.7;

        // EndsWith - medium score
        if (source.EndsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return 0.6;

        // Word boundary match - medium score
        var pattern = $@"\b{Regex.Escape(keyword)}\b";
        if (Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase))
            return 0.6;

        // Levenshtein distance for partial matches
        var distance = CalculateLevenshteinDistance(source.ToLower(), keyword.ToLower());
        var maxLength = Math.Max(source.Length, keyword.Length);
        if (maxLength > 0)
        {
            var similarity = 1.0 - ((double)distance / maxLength);
            if (similarity > 0.5) // Only consider if similarity is above threshold
                return similarity * 0.5; // Lower weight for fuzzy matches
        }

        return 0;
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] matrix = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = target[j - 1] == source[i - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1, // deletion
                    matrix[i, j - 1] + 1), // insertion
                    matrix[i - 1, j - 1] + cost); // substitution
            }
        }

        return matrix[source.Length, target.Length];
    }
}
