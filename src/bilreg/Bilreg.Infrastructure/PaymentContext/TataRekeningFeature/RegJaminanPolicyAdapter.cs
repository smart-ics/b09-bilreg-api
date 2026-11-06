using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Infrastructure adapter implementing <see cref="IRegJaminanPolicy"/> by querying
/// the authoritative Admission context (<see cref="IRegRepo"/>).
/// </summary>
public sealed class RegJaminanPolicyAdapter : IRegJaminanPolicy
{
    private readonly IRegRepo _regRepo;

    public RegJaminanPolicyAdapter(IRegRepo regRepo)
    {
        _regRepo = regRepo ?? throw new ArgumentNullException(nameof(regRepo));
    }

    public bool IsJaminanValidForReg(string regId, string paymentId)
    {
        if (string.IsNullOrWhiteSpace(regId) || string.IsNullOrWhiteSpace(paymentId))
            return false;

        var cleanRegId = regId.Trim();
        var cleanPaymentId = paymentId.Trim();

        var regMayBe = _regRepo.LoadEntity(RegModel.Key(cleanRegId));

        return regMayBe.Match(
            onSome: reg =>
            {
                if (string.IsNullOrWhiteSpace(reg.RegId) || reg.RegId == "-")
                    return false;

                // Registration must not be voided/canceled
                if (reg.RegVoidAudit != AuditInfoType.Default)
                    return false;

                if (reg.TipeJaminan is null || string.IsNullOrWhiteSpace(reg.TipeJaminan.TipeJaminanId))
                    return false;

                return string.Equals(reg.TipeJaminan.TipeJaminanId, cleanPaymentId, StringComparison.OrdinalIgnoreCase);
            },
            onNone: () => false);
    }
}
