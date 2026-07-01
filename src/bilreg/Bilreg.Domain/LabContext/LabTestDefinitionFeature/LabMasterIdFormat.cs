using System.Text.RegularExpressions;

namespace Bilreg.Domain.LabContext.LabTestDefinitionFeature;

public static partial class LabMasterIdFormat
{
    private const string LtdPattern = "^LTD[0-9A-F]{4}$";
    private const string MlcPattern = "^MLC[0-9A-F]{4}$";

    public static bool IsValidLtd(string? testDefinitionId) =>
        !string.IsNullOrWhiteSpace(testDefinitionId)
        && LtdRegex().IsMatch(testDefinitionId);

    public static bool IsValidMlc(string? componentId) =>
        !string.IsNullOrWhiteSpace(componentId)
        && MlcRegex().IsMatch(componentId);

    public static string FormatLtd(int hexValue) =>
        hexValue is < 0 or > 0xFFFF
            ? throw new ArgumentOutOfRangeException(nameof(hexValue), "LTD suffix must fit 4 hex digits.")
            : $"LTD{hexValue:X4}";

    public static int ParseLtdSuffix(string testDefinitionId)
    {
        if (!IsValidLtd(testDefinitionId))
            throw new ArgumentException("LAB_INVALID_LTD_FORMAT: TestDefinitionId must be LTD + 4 uppercase hex.", nameof(testDefinitionId));

        return Convert.ToInt32(testDefinitionId[3..], 16);
    }

    [GeneratedRegex(LtdPattern)]
    private static partial Regex LtdRegex();

    [GeneratedRegex(MlcPattern)]
    private static partial Regex MlcRegex();
}
