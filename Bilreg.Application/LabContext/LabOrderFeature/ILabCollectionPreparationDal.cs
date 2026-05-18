namespace Bilreg.Application.LabContext.LabOrderFeature;

public interface ILabCollectionPreparationDal
{
    LabCollectionPreparationView? Get(string orderId);
}

public record LabCollectionPreparationView(
    string OrderId,
    string OrderNo,
    string PatientId,
    string PatientName,
    int LabOrderStatus,
    IReadOnlyList<LabCollectionPreparationTestItem> Tests,
    IReadOnlyList<LabCollectionPreparationVacutainerGroup> VacutainerGroups);

public record LabCollectionPreparationTestItem(
    int ItemNo,
    string TestId,
    string TestCode,
    string TestName,
    int TubeType,
    string SpecimenType,
    int RequiredTubeCount);

public record LabCollectionPreparationVacutainerGroup(
    int TubeType,
    string SpecimenType,
    int TubeCount);
