using System.Text;
using System.Text.RegularExpressions;

namespace Farinv.Domain.Shared.Helpers;

public static class StringExtensions
{
    public static string ToEyd(this string stringToken)
    {
        if (string.IsNullOrEmpty(stringToken))
            return stringToken;

        var result = stringToken.ToUpper();

        result = Regex.Replace(result, "OE", "U");
        result = Regex.Replace(result, "DJ", "J");
        result = Regex.Replace(result, "TJ", "C");

        return result;
    }    
    
    public static string ToEjaanLama(this string stringToken)
    {
        if (string.IsNullOrEmpty(stringToken))
            return stringToken;

        var result = stringToken.ToUpper();

        result = Regex.Replace(result, "U", "OE");
        result = Regex.Replace(result, "J", "DJ");
        result = Regex.Replace(result, "C", "TJ");

        return result;
    }
    
    public static string ToNormal(this string stringToken)
    {
        if (string.IsNullOrEmpty(stringToken))
            return stringToken;

        var result = stringToken.ToUpper();

        result = Regex.Replace(result, "DH", "D");
        result = Regex.Replace(result, "SY", "S");
        result = Regex.Replace(result, "OO", "U");
        return result;
    }

    
    public static string RemoveDoubleChar(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        var previousChar = '\0';

        foreach (var currentChar in input)
        {
            if (currentChar == previousChar) continue;
            result.Append(currentChar);
            previousChar = currentChar;
        }

        return result.ToString();
    }
}
