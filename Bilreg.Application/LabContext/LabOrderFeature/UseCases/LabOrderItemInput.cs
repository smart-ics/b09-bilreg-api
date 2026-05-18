using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderItemInput(
    string TestId,
    string TestCode,
    string TestName,
    string TarifId,
    string TarifCode,
    string TarifName,
    VacutainerTypeEnum TubeType,
    string SpecimenType,
    int RequiredTubeCount)
{
    public LabOrderItemModel ToModel()
        => LabOrderItemModel.Create(
            TestId,
            TestCode,
            TestName,
            TarifId,
            TarifCode,
            TarifName,
            TubeType,
            SpecimenType,
            RequiredTubeCount);
}
