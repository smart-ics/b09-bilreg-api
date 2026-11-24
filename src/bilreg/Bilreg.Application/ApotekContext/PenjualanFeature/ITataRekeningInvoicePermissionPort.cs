using Bilreg.Domain.ApotekContext.PenjualanFeature;

namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Port abstraksi untuk mengevaluasi izin mutabilitas tagihan penjualan farmasi
/// sesuai aturan ADR-APT-003 sebelum operasi pembatalan (soft-void) dilakukan.
/// </summary>
public interface ITataRekeningInvoicePermissionPort
{
    /// <summary>
    /// Memeriksa apakah baris pembebanan di Tata Rekening (ta_trs_billing) masih berstatus mutabel.
    /// Mengembalikan false jika rekening billing pasien telah ditutup/final.
    /// </summary>
    bool AllowsModification(string tataRekeningChargeId);

    /// <summary>
    /// Memeriksa apakah transaksi penjualan model tertentu diizinkan untuk dibatalkan (soft-void)
    /// berdasarkan status tagihan, rekening Tata Rekening, dan shift kasir jika penjualan bebas (Jenis 3).
    /// </summary>
    bool AllowsVoid(PenjualanModel model);
}

/// <summary>
/// Implementasi penolakan mutabilitas default (fail-closed) untuk keamanan finansial.
/// </summary>
public class DenyTataRekeningInvoicePermissionPort : ITataRekeningInvoicePermissionPort
{
    public bool AllowsModification(string tataRekeningChargeId) => false;
    public bool AllowsVoid(PenjualanModel model) => false;
}

/// <summary>
/// Implementasi pengizinan mutabilitas untuk pengujian unconstrained.
/// </summary>
public class AllowTataRekeningInvoicePermissionPort : ITataRekeningInvoicePermissionPort
{
    public bool AllowsModification(string tataRekeningChargeId) => true;
    public bool AllowsVoid(PenjualanModel model) => true;
}
