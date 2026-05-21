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
    int RequiredTubeCount)
{
    public static LabOrderItemModel Create(
        string testDefinitionId,
        string labTestCode,
        string labTestName,
        string tarifId,
        string tarifCode,
        string tarifName,
        VacutainerTypeEnum tubeType,
        string specimenType,
        int requiredTubeCount)
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
            RequiredTubeCount: requiredTubeCount);
}
