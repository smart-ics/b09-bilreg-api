namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabResultVerificationWorklistDal
{
    IEnumerable<LabResultVerificationWorklistView> List(LabResultVerificationWorklistFilter filter);
}

public record LabResultVerificationWorklistFilter(
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2);

public record LabResultVerificationWorklistView(
    string OrderId,
    string OrderNo,
    string PatientId,
    string PatientName,
    DateTime RecordedDate,
    int ResultStatus,
    string RecordedUserId);
