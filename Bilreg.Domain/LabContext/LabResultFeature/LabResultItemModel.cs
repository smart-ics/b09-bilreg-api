namespace Bilreg.Domain.LabContext.LabResultFeature;

public record LabResultItemModel(
    int ItemNo,
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
    string ReferenceRangeText,
    LabResultFlagEnum FlagStatus);
