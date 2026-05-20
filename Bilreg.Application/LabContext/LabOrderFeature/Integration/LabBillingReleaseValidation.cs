using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingReleaseValidationRequest(
    string OrderId,
    string OrderNo,
    string UserId);

public record LabBillingReleaseValidationResult(
    BillingReleaseStatusEnum Status,
    string Message);
