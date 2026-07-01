using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public record LabResultItemDto(
    string ResultDocumentId,
    int ItemNo,
    string ComponentId,
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    int SequenceNo,
    int ResultType,
    decimal NumericValue,
    string TextValue,
    string OptionValue,
    string NarrativeValue,
    string Unit,
    string ReferenceRangeText,
    bool IsMandatory,
    int FlagStatus)
{
    public static LabResultItemDto FromModel(string resultDocumentId, LabResultItemModel model)
        => new(
            resultDocumentId,
            model.ItemNo,
            model.ComponentId,
            model.TestId,
            model.TestName,
            model.ComponentCode,
            model.ComponentName,
            model.SequenceNo,
            (int)model.ResultType,
            model.NumericValue,
            model.TextValue,
            model.OptionValue,
            model.NarrativeValue,
            model.Unit,
            model.ReferenceRangeText,
            model.IsMandatory,
            (int)model.FlagStatus);

    public LabResultItemModel ToModel()
        => new(
            ItemNo,
            ComponentId,
            TestId,
            TestName,
            ComponentCode,
            ComponentName,
            SequenceNo,
            (LabResultTypeEnum)ResultType,
            NumericValue,
            TextValue,
            OptionValue,
            NarrativeValue,
            Unit,
            ReferenceRangeText,
            IsMandatory,
            (LabResultFlagEnum)FlagStatus);
}
