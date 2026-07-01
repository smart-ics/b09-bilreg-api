using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public record LabOrderItemComponentDto(
    string OrderId,
    int ItemNo,
    int ComponentNo,
    string ComponentId,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    string Unit,
    string ReferenceRangeText,
    int SequenceNo,
    bool IsMandatory)
{
    public static LabOrderItemComponentDto FromModel(string orderId, LabOrderItemComponentModel model)
        => new(
            orderId,
            model.ItemNo,
            model.ComponentNo,
            model.ComponentId,
            model.ComponentCode,
            model.ComponentName,
            model.ResultType,
            model.Unit,
            model.ReferenceRangeText,
            model.SequenceNo,
            model.IsMandatory);

    public LabOrderItemComponentModel ToModel()
        => new(
            ItemNo,
            ComponentNo,
            ComponentId,
            ComponentCode,
            ComponentName,
            ResultType,
            Unit,
            ReferenceRangeText,
            SequenceNo,
            IsMandatory);
}
