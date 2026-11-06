using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public record LabOrderItemDto(
    string OrderId,
    int ItemNo,
    string TestDefinitionId,
    string LabTestCode,
    string LabTestName,
    string TarifId,
    string TarifCode,
    string TarifName,
    int TubeType,
    string SpecimenType,
    int RequiredTubeCount,
    int Urgency = 0,
    int ServiceType = 1,
    string DisciplineId = "",
    string DisciplineName = "",
    string TestGroupId = "",
    string TestGroupName = "")
{
    public static LabOrderItemDto FromModel(string orderId, LabOrderItemModel model)
        => new(
            orderId,
            model.ItemNo,
            model.TestDefinitionId,
            model.LabTestCode,
            model.LabTestName,
            model.TarifId,
            model.TarifCode,
            model.TarifName,
            (int)model.TubeType,
            model.SpecimenType,
            model.RequiredTubeCount,
            (int)model.Urgency,
            (int)model.ServiceType,
            model.DisciplineId,
            model.DisciplineName,
            model.TestGroupId,
            model.TestGroupName);

    public LabOrderItemModel ToModel()
        => new(
            ItemNo,
            TestDefinitionId,
            LabTestCode,
            LabTestName,
            TarifId,
            TarifCode,
            TarifName,
            (VacutainerTypeEnum)TubeType,
            SpecimenType,
            RequiredTubeCount,
            (LabOrderUrgencyEnum)Urgency,
            (LaboratoryServiceType)ServiceType,
            DisciplineId,
            DisciplineName,
            TestGroupId,
            TestGroupName);
}
