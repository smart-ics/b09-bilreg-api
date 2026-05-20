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

    public LabBillingReleaseValidationResult ValidateReleaseEligibility(LabBillingReleaseValidationRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        if (request.OrderNo.Contains("BLOCK", StringComparison.OrdinalIgnoreCase))
        {
            return new LabBillingReleaseValidationResult(
                BillingReleaseStatusEnum.Blocked,
                "Tagihan pasien belum memenuhi syarat release.");
        }

        return new LabBillingReleaseValidationResult(BillingReleaseStatusEnum.Clear, "");
    }
}
