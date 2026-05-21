namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record BillingReleaseValidationResult(
    BillingReleaseValidationCode Code,
    string Message);
