using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Test.LabContext.LabResultFeature;

internal static class LabResultTestSupport
{
    public static LabResultItemCapture Capture(
        string componentId = "MLC0001",
        string testId = "LTD0001",
        string testName = "Hemoglobin",
        LabResultTypeEnum type = LabResultTypeEnum.Numeric,
        decimal numeric = 14m,
        string refText = "12-16",
        bool isMandatory = true)
        => new(
            componentId,
            testId,
            testName,
            "HB",
            "Hemoglobin",
            1,
            type,
            numeric,
            "",
            "",
            "",
            "g/dL",
            refText,
            isMandatory);
}
