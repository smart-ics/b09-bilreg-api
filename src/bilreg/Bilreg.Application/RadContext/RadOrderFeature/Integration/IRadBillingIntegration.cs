using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature.Integration;

public interface IRadBillingIntegration
{
    string ChargeOrder(RadOrderModel order, string userId, DateTime occurredAt);
    void CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt);
    void CancelCharge(RadBillingCancelRequest request);
}
