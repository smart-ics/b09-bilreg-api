using System.Globalization;
using System.Text.RegularExpressions;

namespace Bilreg.Domain.LabContext.LabResultFeature;

public static class LabResultFlagging
{
    /// <summary>
    /// Pragmatic auto-flag: numeric vs simple reference text only; otherwise Normal.
    /// </summary>
    public static LabResultFlagEnum Compute(LabResultTypeEnum resultType, decimal numericValue, string referenceRangeText)
    {
        if (resultType != LabResultTypeEnum.Numeric)
            return LabResultFlagEnum.Normal;

        var text = (referenceRangeText ?? string.Empty).Trim();
        if (text.Length == 0)
            return LabResultFlagEnum.Normal;

        text = text.Replace('–', '-').Replace('—', '-');

        // Range: "3.5-5.5" or "3.5 - 5.5"
        var rangeMatch = Regex.Match(text, @"^\s*(\d+(?:\.\d+)?)\s*-\s*(\d+(?:\.\d+)?)\s*$");
        if (rangeMatch.Success
            && decimal.TryParse(rangeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lo)
            && decimal.TryParse(rangeMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var hi)
            && hi >= lo)
        {
            if (numericValue < lo)
                return LabResultFlagEnum.Low;
            if (numericValue > hi)
                return LabResultFlagEnum.High;
            return LabResultFlagEnum.Normal;
        }

        // Upper bound: "< 5" or "<5" or "≤ 5"
        var ltMatch = Regex.Match(text, @"^\s*[<≤]\s*(\d+(?:\.\d+)?)\s*$");
        if (ltMatch.Success
            && decimal.TryParse(ltMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var upper))
        {
            if (numericValue >= upper)
                return LabResultFlagEnum.High;
            return LabResultFlagEnum.Normal;
        }

        // Lower bound: "> 10" or ">10"
        var gtMatch = Regex.Match(text, @"^\s*[>≥]\s*(\d+(?:\.\d+)?)\s*$");
        if (gtMatch.Success
            && decimal.TryParse(gtMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lower))
        {
            if (numericValue <= lower)
                return LabResultFlagEnum.Low;
            return LabResultFlagEnum.Normal;
        }

        return LabResultFlagEnum.Normal;
    }
}
