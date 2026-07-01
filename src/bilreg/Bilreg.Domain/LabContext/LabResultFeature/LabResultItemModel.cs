namespace Bilreg.Domain.LabContext.LabResultFeature;

public record LabResultItemModel(
    int ItemNo,
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
    bool IsMandatory,
    LabResultFlagEnum FlagStatus);
