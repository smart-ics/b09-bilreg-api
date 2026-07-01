namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingReleaseValidationRequest(
    string OrderId,
    string OrderNo,
    string BillingTindakanId);
