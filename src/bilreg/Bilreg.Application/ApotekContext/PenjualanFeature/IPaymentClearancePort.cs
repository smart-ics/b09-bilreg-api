namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Bukti sah pelunasan kuitansi pembayaran kasir apotek / kasir sentral rumah sakit.
/// </summary>
public record PaymentClearanceEvidence(
    string PaymentClearanceReff,
    DateTime ClearedAt,
    decimal AmountPaid = 0,
    string KasirId = "",
    string PaymentType = "");

/// <summary>
/// Port abstraksi untuk memvalidasi bukti pelunasan kuitansi kasir sebelum penyerahan obat fisik ke pasien.
/// </summary>
public interface IPaymentClearancePort
{
    /// <summary>
    /// Memuat data bukti pembayaran berdasarkan nomor referensi kuitansi kasir (fs_kd_trs_kwitansi).
    /// </summary>
    PaymentClearanceEvidence? Load(string paymentClearanceReff);

    /// <summary>
    /// Memeriksa apakah nomor referensi kuitansi telah dinyatakan lunas oleh kasir.
    /// </summary>
    bool IsCleared(string paymentClearanceReff);
}

/// <summary>
/// Implementasi fail-closed default bila port pembayaran kasir belum terhubung.
/// </summary>
public class FailClosedPaymentClearancePort : IPaymentClearancePort
{
    public PaymentClearanceEvidence? Load(string paymentClearanceReff) => null;
    public bool IsCleared(string paymentClearanceReff) => false;
}
