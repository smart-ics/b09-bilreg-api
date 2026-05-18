using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;

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
}
