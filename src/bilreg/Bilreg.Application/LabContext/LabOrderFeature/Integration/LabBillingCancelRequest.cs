namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingCancelRequest(
    string OrderId,
    string RegId,
    string UserId,
    string Reason,
    DateTime CancelledAt);
