using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Infrastructure adapter implementing <see cref="IRegJaminanPolicy"/> by querying
/// the authoritative Admission context (<see cref="IRegRepo"/> and <see cref="ITipeJaminanRepo"/>).
/// </summary>
public sealed class RegJaminanPolicyAdapter : IRegJaminanPolicy
{
    private readonly IRegRepo _regRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;

    public RegJaminanPolicyAdapter(IRegRepo regRepo, ITipeJaminanRepo tipeJaminanRepo)
    {
        _regRepo = regRepo ?? throw new ArgumentNullException(nameof(regRepo));
        _tipeJaminanRepo = tipeJaminanRepo ?? throw new ArgumentNullException(nameof(tipeJaminanRepo));
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

                // "00000" (Bayar Sendiri / UMUM) is not a jaminan/insurance payment
                if (string.Equals(cleanPaymentId, "00000", StringComparison.OrdinalIgnoreCase))
                    return false;

                var jaminanMayBe = _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(cleanPaymentId));
                return jaminanMayBe.Match(
                    onSome: jaminan => jaminan.IsAktif,
                    onNone: () => false);
            },
            onNone: () => false);
    }
}
