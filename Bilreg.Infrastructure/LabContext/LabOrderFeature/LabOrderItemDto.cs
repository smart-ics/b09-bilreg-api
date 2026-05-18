using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public record LabOrderItemDto(
    string OrderId,
    int ItemNo,
    string TestId,
    string TestCode,
    string TestName,
    string TarifId,
    string TarifCode,
    string TarifName,
    int TubeType,
    string SpecimenType,
    int RequiredTubeCount)
{
    public static LabOrderItemDto FromModel(string orderId, LabOrderItemModel model)
        => new(
            orderId,
            model.ItemNo,
            model.TestId,
            model.TestCode,
            model.TestName,
            model.TarifId,
            model.TarifCode,
            model.TarifName,
            (int)model.TubeType,
            model.SpecimenType,
            model.RequiredTubeCount);

    public LabOrderItemModel ToModel()
        => new(
            ItemNo,
            TestId,
            TestCode,
            TestName,
            TarifId,
            TarifCode,
            TarifName,
            (VacutainerTypeEnum)TubeType,
            SpecimenType,
            RequiredTubeCount);
}
