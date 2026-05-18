namespace Bilreg.Application.LabContext.LabOrderFeature;

public record LabOrderReleaseWorklistFilter(
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2);

public record LabReleaseView(
    string OrderId,
    string OrderNo,
    string PatientName,
    DateTime VerifiedDate,
    int FinancialClearance,
    DateTime ReleasedDate,
    int LabOrderStatus);

public interface ILabOrderReleaseWorklistDal
{
    IEnumerable<LabReleaseView> List(LabOrderReleaseWorklistFilter filter);
}
