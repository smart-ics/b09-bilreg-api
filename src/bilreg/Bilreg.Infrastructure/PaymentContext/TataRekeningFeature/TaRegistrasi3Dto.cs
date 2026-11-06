using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record TaRegistrasi3Dto(
    string fs_kd_reg,
    string fs_kd_bayar,
    string fs_nm_bayar,
    decimal fn_jasa,
    decimal fn_obat,
    string fs_kd_rek,
    bool fb_subsidi)
{
    public const string DefaultKasirCoaId = "1499901";
    public const string DefaultKasirCoaName = "Rekening Piutang Kasir Perantara";

    public static TaRegistrasi3Dto FromModel(string regId, TataRekeningPaymentType model)
    {
        var coaId = model.Coa?.CoaId ?? string.Empty;
        if (string.Equals(model.Payment.PaymentId, PaymentType.ByKas.PaymentId, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(coaId) ||
                string.Equals(coaId.Trim(), "BYKAS", StringComparison.OrdinalIgnoreCase) ||
                coaId.Trim() == "-")
            {
                coaId = DefaultKasirCoaId;
            }
        }

        return new(
            regId,
            model.Payment.PaymentId,
            model.Payment.PaymentName,
            model.NilaiJasa,
            model.NilaiObat,
            coaId,
            model.Payment.IsTipeJaminan);
    }

    public TataRekeningPaymentType ToModel()
    {
        var paymentName = string.IsNullOrWhiteSpace(fs_nm_bayar)
            ? ResolvePaymentName(fs_kd_bayar)
            : fs_nm_bayar;
        var isTipeJaminan = ResolveIsTipeJaminan(fs_kd_bayar);
        var payment = new PaymentType(fs_kd_bayar, paymentName, isTipeJaminan);

        var coaId = fs_kd_rek;
        var coaName = string.Empty;
        if (string.Equals(fs_kd_bayar, PaymentType.ByKas.PaymentId, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(coaId) ||
                string.Equals(coaId.Trim(), "BYKAS", StringComparison.OrdinalIgnoreCase) ||
                coaId.Trim() == "-")
            {
                coaId = DefaultKasirCoaId;
                coaName = DefaultKasirCoaName;
            }
            else if (coaId.Trim() == DefaultKasirCoaId)
            {
                coaName = DefaultKasirCoaName;
            }
        }

        var coa = string.IsNullOrWhiteSpace(coaId)
            ? CoaType.Default
            : new CoaType(coaId, coaName);
        return new TataRekeningPaymentType(payment, fn_jasa, fn_obat, coa);
    }

    private static bool ResolveIsTipeJaminan(string paymentId) =>
        paymentId is not ("BYKAS" or "BYPRI" or "BYDPU" or "BYVCH" or "BYDPK");

    private static string ResolvePaymentName(string paymentId) => paymentId switch
    {
        "BYKAS" => PaymentType.ByKas.PaymentName,
        "BYPRI" => PaymentType.ByPri.PaymentName,
        "BYDPU" => PaymentType.ByDpu.PaymentName,
        "BYVCH" => PaymentType.ByVch.PaymentName,
        "BYDPK" => PaymentType.ByDpk.PaymentName,
        _ => paymentId
    };
}
