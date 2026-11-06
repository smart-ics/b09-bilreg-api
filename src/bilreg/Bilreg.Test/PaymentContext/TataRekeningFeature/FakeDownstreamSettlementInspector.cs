using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal sealed class FakeDownstreamSettlementInspector(
    bool hasApprovedAllocation = false,
    bool hasCashierSettlement = false) : IDownstreamSettlementInspector
{
    public bool HasApprovedAllocation(string regId) => hasApprovedAllocation;

    public bool HasCashierSettlement(string regId) => hasCashierSettlement;
}
