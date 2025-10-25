using System.Text.RegularExpressions;

namespace Bilreg.Domain.Helpers;

public static class StringExtensions
{
    public static string NormalizeToEyd(this string personName)
    {
        if (string.IsNullOrEmpty(personName))
            return personName;

        var normalized = personName.ToLower();

        // Common old-to-new spelling transformations
        normalized = Regex.Replace(normalized, "oe", "u");
        normalized = Regex.Replace(normalized, "tj", "c");
        normalized = Regex.Replace(normalized, "dj", "j");
        normalized = Regex.Replace(normalized, "dh", "d");

        return normalized;
    }    
}