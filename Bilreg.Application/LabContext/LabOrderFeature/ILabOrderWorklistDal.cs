namespace Bilreg.Application.LabContext.LabOrderFeature;

public interface ILabOrderWorklistDal
{
    IEnumerable<LabOrderWorklistView> List(LabOrderWorklistFilter filter);
}

public record LabOrderWorklistFilter(
    int? LabOrderStatus,
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2);

public record LabOrderWorklistView(
    string OrderId,
    string OrderNo,
    int LabOrderStatus,
    int OrderSource,
    string PatientId,
    string PatientName,
    string Gender,
    int AgeAtOrder,
    int ItemCount,
    int LastBillingReleaseStatus,
    int OwareStatus,
    DateTime CrtDate,
    string BillingTindakanId,
    IReadOnlyList<string>? TestNames = null);
