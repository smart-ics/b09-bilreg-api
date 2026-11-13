namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Hasil pemeriksaan status pelunasan kasir atau keabsahan klaim BPJS.
/// </summary>
public record PaymentClearanceCheckResult(
    bool IsCleared,
    string ClearanceType,
    string Message);

/// <summary>
/// Port eksternal untuk validasi pelunasan tagihan kasir (pasien mandiri/umum)
/// atau keabsahan nomor SEP (pasien BPJS) sebelum penyerahan obat fisik di Loket 2.
/// </summary>
public interface IPaymentClearancePort
{
    /// <summary>
    /// Memeriksa apakah transaksi tagihan penjualan atau klaim BPJS telah dinyatakan lunas / valid.
    /// </summary>
    bool IsBillCleared(string penjualanId, string noSep);

    /// <summary>
    /// Memeriksa rincian status pelunasan atau keabsahan klaim dengan keterangan deskriptif.
    /// </summary>
    PaymentClearanceCheckResult CheckClearance(string penjualanId, string noSep);
}

/// <summary>
/// Implementasi default fail-closed jika adapter pembayaran belum diintegrasikan.
/// </summary>
public class FailClosedPharmacyPaymentClearancePort : IPaymentClearancePort
{
    public bool IsBillCleared(string penjualanId, string noSep) => false;

    public PaymentClearanceCheckResult CheckClearance(string penjualanId, string noSep)
        => new(false, "FailClosed", "Payment clearance verification is not available or closed by default.");
}
