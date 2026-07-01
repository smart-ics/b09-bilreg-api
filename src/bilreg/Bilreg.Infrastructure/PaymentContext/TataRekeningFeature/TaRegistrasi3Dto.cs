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
    public static TaRegistrasi3Dto FromModel(string regId, TataRekeningPaymentType model) =>
        new(
            regId,
            model.Payment.PaymentId,
            model.Payment.PaymentName,
            model.NilaiJasa,
            model.NilaiObat,
            model.Coa.CoaId,
            model.Payment.IsTipeJaminan);

    public TataRekeningPaymentType ToModel()
    {
        var paymentName = string.IsNullOrWhiteSpace(fs_nm_bayar)
            ? ResolvePaymentName(fs_kd_bayar)
            : fs_nm_bayar;
        var isTipeJaminan = ResolveIsTipeJaminan(fs_kd_bayar);
        var payment = new PaymentType(fs_kd_bayar, paymentName, isTipeJaminan);
        var coa = string.IsNullOrWhiteSpace(fs_kd_rek)
            ? CoaType.Default
            : new CoaType(fs_kd_rek, "");
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
