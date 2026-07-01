namespace Bilreg.Domain.LabContext.LabResultFeature;

/// <summary>
/// Fully resolved result line (server scaffold + client value) for aggregate persistence.
/// </summary>
public record LabResultItemCapture(
    string ComponentId,
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    int SequenceNo,
    LabResultTypeEnum ResultType,
    decimal NumericValue,
    string TextValue,
    string OptionValue,
    string NarrativeValue,
    string Unit,
    string ReferenceRangeText,
    bool IsMandatory);
