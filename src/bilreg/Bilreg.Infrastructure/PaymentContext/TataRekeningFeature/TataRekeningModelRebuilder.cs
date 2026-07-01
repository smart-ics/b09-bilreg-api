using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

internal static class TataRekeningModelRebuilder
{
    public static TataRekeningModel WithPayments(
        TataRekeningModel source,
        IEnumerable<TataRekeningPaymentType> payments) =>
        new(
            source.RegId,
            source.Status,
            source.FinalizationInfo,
            payments,
            source.ListTrsBill,
            source.FinancialVerificationStatus,
            source.FinancialVerificationInfo,
            source.IsFinancialResponsibilityAllocated,
            source.SettlementInitiated,
            source.Version);
}
