namespace Bilreg.Application.RadContext.RadOrderFeature.Integration;

public record RadBillingChargeResult(
    bool Success,
    string? BillingTindakanId,
    string? ErrorMessage);
