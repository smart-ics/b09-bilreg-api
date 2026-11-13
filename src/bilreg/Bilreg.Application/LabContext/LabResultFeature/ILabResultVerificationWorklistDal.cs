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
    string RecordedUserId,
    bool IsCito = false,
    string Gender = "",
    int AgeAtOrder = 0,
    string LayananPerujukName = "",
    string DokterPerujukName = "",
    string TestNames = "",
    bool HasCriticalValue = false,
    int VersionNo = 1,
    int ResultSource = 0);
