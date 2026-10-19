using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Infrastructure.LabContext.Integration;

public class LabBillingIntegration : ILabBillingIntegration
{
    private static int _fakeSequence;

    public string CreateTindakan(LabBillingChargeRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var seq = Interlocked.Increment(ref _fakeSequence);
        return $"TDK-FAKE-{seq:D4}";
    }

    public string ChargeOrder(LabOrderModel order, string userId, DateTime occurredAt)
    {
        Guard.Against.Null(order);
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var seq = Interlocked.Increment(ref _fakeSequence);
        return $"TDK-FAKE-{seq:D4}";
    }

    public void CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
    }

    public void CancelCharge(LabBillingCancelRequest request)
    {
        Guard.Against.Null(request);
        CancelCharge(request.OrderId, request.RegId, request.UserId, request.Reason, request.CancelledAt);
    }

    public BillingReleaseValidationResult ValidateReleaseEligibility(LabBillingReleaseValidationRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.OrderNo, nameof(request.OrderNo));

        if (string.Equals(request.OrderId, "BLOCK-INFRA", StringComparison.Ordinal))
            throw new LabBillingReleaseValidationException("BIL release validation unavailable.");

        if (request.OrderNo.EndsWith("-BILBLOCK", StringComparison.OrdinalIgnoreCase))
        {
            return new BillingReleaseValidationResult(
                BillingReleaseValidationCode.Blocked,
                "Tagihan pasien belum memenuhi syarat release.");
        }

        return new BillingReleaseValidationResult(BillingReleaseValidationCode.Clear, "");
    }
}
