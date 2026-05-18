namespace Bilreg.Domain.LabContext.LabResultFeature;

/// <summary>
/// Input line for recording (no ItemNo; assigned by aggregate).
/// </summary>
public record LabResultItemCapture(
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    LabResultTypeEnum ResultType,
    decimal NumericValue,
    string TextValue,
    string OptionValue,
    string NarrativeValue,
    string Unit,
    string ReferenceRangeText);
