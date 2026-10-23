namespace Bilreg.Application.LabContext.LabOrderFeature;

public record LabOrderActiveTestDto(
    string OrderId,
    string OrderNo,
    string TestDefinitionId,
    string LabTestCode,
    string LabTestName,
    DateTime OrderDate,
    int LabOrderStatus);

public interface ILabOrderDal
{
    IEnumerable<LabOrderActiveTestDto> ListActiveTestsByPatientWithinHours(string patientId, int hours, DateTime asOf);
}
