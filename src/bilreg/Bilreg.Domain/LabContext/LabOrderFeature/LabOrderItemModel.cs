namespace Bilreg.Domain.LabContext.LabOrderFeature;

public record LabOrderItemModel(
    int ItemNo,
    string TestDefinitionId,
    string LabTestCode,
    string LabTestName,
    string TarifId,
    string TarifCode,
    string TarifName,
    VacutainerTypeEnum TubeType,
    string SpecimenType,
    int RequiredTubeCount,
    LabOrderUrgencyEnum Urgency = LabOrderUrgencyEnum.Rutin,
    LaboratoryServiceType ServiceType = LaboratoryServiceType.ClinicalPathology,
    string DisciplineId = "",
    string DisciplineName = "",
    string TestGroupId = "",
    string TestGroupName = "")
{
    public bool IsCito => Urgency == LabOrderUrgencyEnum.Cito;

    public static LabOrderItemModel Create(
        string testDefinitionId,
        string labTestCode,
        string labTestName,
        string tarifId,
        string tarifCode,
        string tarifName,
        VacutainerTypeEnum tubeType,
        string specimenType,
        int requiredTubeCount,
        LabOrderUrgencyEnum urgency = LabOrderUrgencyEnum.Rutin,
        LaboratoryServiceType serviceType = LaboratoryServiceType.ClinicalPathology,
        string disciplineId = "",
        string disciplineName = "",
        string testGroupId = "",
        string testGroupName = "")
        => new(
            ItemNo: 0,
            TestDefinitionId: testDefinitionId,
            LabTestCode: labTestCode,
            LabTestName: labTestName,
            TarifId: tarifId,
            TarifCode: tarifCode,
            TarifName: tarifName,
            TubeType: tubeType,
            SpecimenType: specimenType,
            RequiredTubeCount: requiredTubeCount,
            Urgency: urgency,
            ServiceType: serviceType,
            DisciplineId: disciplineId ?? string.Empty,
            DisciplineName: disciplineName ?? string.Empty,
            TestGroupId: testGroupId ?? string.Empty,
            TestGroupName: testGroupName ?? string.Empty);
}
