using CommunityToolkit.Diagnostics;
using System.Globalization;

namespace Bilreg.Application.Helpers;

public static class GuardString
{
    public static void IsValidDateYmd(this string dateString)
    {
        var isValidFormat = DateTime.TryParseExact(dateString, "yyyy-MM-dd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        Guard.IsTrue(isValidFormat, dateString, $"The {dateString} must be in the format 'yyyy-MM-dd'.");
    }
}
