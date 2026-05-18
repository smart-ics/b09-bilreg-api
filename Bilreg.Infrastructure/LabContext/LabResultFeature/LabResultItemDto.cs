using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public record LabResultItemDto(
    string ResultDocumentId,
    int ItemNo,
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    decimal NumericValue,
    string TextValue,
    string OptionValue,
    string NarrativeValue,
    string Unit,
    string ReferenceRangeText,
    int FlagStatus)
{
    public static LabResultItemDto FromModel(string resultDocumentId, LabResultItemModel model)
        => new(
            resultDocumentId,
            model.ItemNo,
            model.TestId,
            model.TestName,
            model.ComponentCode,
            model.ComponentName,
            (int)model.ResultType,
            model.NumericValue,
            model.TextValue,
            model.OptionValue,
            model.NarrativeValue,
            model.Unit,
            model.ReferenceRangeText,
            (int)model.FlagStatus);

    public LabResultItemModel ToModel()
        => new(
            ItemNo,
            TestId,
            TestName,
            ComponentCode,
            ComponentName,
            (LabResultTypeEnum)ResultType,
            NumericValue,
            TextValue,
            OptionValue,
            NarrativeValue,
            Unit,
            ReferenceRangeText,
            (LabResultFlagEnum)FlagStatus);
}
