using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Adapter yang mengimplementasikan ITrkBillingChargePort untuk menyisipkan dan membatalkan
/// baris pembebanan finansial farmasi rawat inap pada ta_trs_billing melalui ITrsBillingDal.
/// </summary>
public class TrkBillingChargeAdapter : ITrkBillingChargePort
{
    private readonly ITrsBillingDal _trsBillingDal;

    public TrkBillingChargeAdapter(ITrsBillingDal trsBillingDal)
    {
        _trsBillingDal = trsBillingDal;
    }

    public Task<string> PostBillingChargeAsync(
        TrkBillingChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PostBillingCharge(request));
    }

    public string PostBillingCharge(TrkBillingChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var chargeId = NunaId.NewLegacy("BI", 'A');
        var tglTrs = request.TransactionDate.ToString("yyyy-MM-dd");
        var jamTrs = request.TransactionDate.ToString("HH:mm:ss");
        var tglJamTrs = request.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss");

        var ket = string.IsNullOrWhiteSpace(request.Keterangan)
            ? $"Penjualan Farmasi {request.PenjualanId}"
            : request.Keterangan;
        var keterangan = ket.Length > 40 ? ket[..40] : ket;

        var dto = new TrsBillingDto(
            fs_kd_trs: chargeId,
            fn_modul: 1, // 1 = Modul Obat / Farmasi
            fd_tgl_trs: tglTrs,
            fs_jam_trs: jamTrs,
            fd_tgl_jam_trs: tglJamTrs,
            fs_kd_reg: request.RegId ?? string.Empty,
            fs_kd_layanan: request.LayananId ?? string.Empty,
            fs_kd_kelas: request.KelasId ?? string.Empty,
            fs_kd_rekap_cetak: string.Empty,
            fs_kd_petugas: request.PetugasId ?? string.Empty,
            fn_sub_total: request.SubTotal,
            fn_diskon: request.Diskon,
            fn_biaya: request.Biaya,
            fn_tax: request.Tax,
            fn_total: request.Total,
            fs_keterangan: keterangan,
            fs_keterangan2: string.Empty,
            fs_kd_ref_biaya: request.PenjualanId ?? string.Empty,
            fn_qty: 1,
            fs_kd_trs_main: string.Empty,
            fs_mr: string.Empty,
            fs_nm_pasien: string.Empty,
            fs_nm_layanan: string.Empty,
            fs_nm_kelas: string.Empty,
            fs_nm_rekap_cetak: string.Empty);

        _trsBillingDal.Insert(dto);
        return chargeId;
    }

    public Task CancelBillingChargeAsync(
        string chargeId,
        string actorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        CancelBillingCharge(chargeId, actorId, reason);
        return Task.CompletedTask;
    }

    public void CancelBillingCharge(string chargeId, string actorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
            return;

        _trsBillingDal.Delete(TrsBillType.Key(chargeId));
    }
}
