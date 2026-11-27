namespace Bilreg.Application.RadContext.RadOrderFeature.Integration;

public record RadBillingCancelRequest(
    string OrderId,
    string RegId,
    string UserId,
    string Reason,
    DateTime CancelledAt);
