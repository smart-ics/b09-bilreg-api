namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingChargeRequest(string OrderId, string UserId);

public interface ILabBillingIntegration
{
    string CreateTindakan(LabBillingChargeRequest request);

    LabBillingReleaseValidationResult ValidateReleaseEligibility(LabBillingReleaseValidationRequest request);
}
