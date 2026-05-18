namespace Bilreg.Domain.LabContext.LabOrderFeature;

public record LabOrderItemModel(
    int ItemNo,
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
    public static LabOrderItemModel Create(
        string testId,
        string testCode,
        string testName,
        string tarifId,
        string tarifCode,
        string tarifName,
        VacutainerTypeEnum tubeType,
        string specimenType,
        int requiredTubeCount)
        => new(
            ItemNo: 0,
            TestId: testId,
            TestCode: testCode,
            TestName: testName,
            TarifId: tarifId,
            TarifCode: tarifCode,
            TarifName: tarifName,
            TubeType: tubeType,
            SpecimenType: specimenType,
            RequiredTubeCount: requiredTubeCount);
}
