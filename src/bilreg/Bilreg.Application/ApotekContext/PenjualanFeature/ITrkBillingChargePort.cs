namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Data transfer object permintaan pembebanan finansial farmasi rawat inap ke akun rekening pasien.
/// </summary>
public record TrkBillingChargeRequest(
    string PenjualanId,
    string RegId,
    string LayananId,
    string KelasId,
    string PetugasId,
    DateTime TransactionDate,
    decimal SubTotal,
    decimal Diskon,
    decimal Biaya,
    decimal Tax,
    decimal Total,
    string Keterangan = "");

/// <summary>
/// Port abstraksi untuk mengirimkan pembebanan biaya farmasi rawat inap ke akun ta_trs_billing di Tata Rekening
/// dan membatalkan baris pembebanan jika transaksi penjualan dibatalkan (soft-void).
/// </summary>
public interface ITrkBillingChargePort
{
    /// <summary>
    /// Menerbitkan pembebanan ke ta_trs_billing secara asinkron, mengembalikan nomor charge unik (fs_kd_trs).
    /// </summary>
    Task<string> PostBillingChargeAsync(
        TrkBillingChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Menerbitkan pembebanan ke ta_trs_billing secara sinkron, mengembalikan nomor charge unik (fs_kd_trs).
    /// </summary>
    string PostBillingCharge(TrkBillingChargeRequest request);

    /// <summary>
    /// Membatalkan baris pembebanan di ta_trs_billing secara asinkron saat transaksi penjualan dibatalkan.
    /// </summary>
    Task CancelBillingChargeAsync(
        string chargeId,
        string actorId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Membatalkan baris pembebanan di ta_trs_billing secara sinkron saat transaksi penjualan dibatalkan.
    /// </summary>
    void CancelBillingCharge(string chargeId, string actorId, string reason);
}

/// <summary>
/// Implementasi fail-closed default bila port pembebanan billing belum dikonfigurasi.
/// </summary>
public class FailClosedTrkBillingChargePort : ITrkBillingChargePort
{
    public Task<string> PostBillingChargeAsync(
        TrkBillingChargeRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromException<string>(new InvalidOperationException("ITrkBillingChargePort adapter is not configured."));

    public string PostBillingCharge(TrkBillingChargeRequest request)
        => throw new InvalidOperationException("ITrkBillingChargePort adapter is not configured.");

    public Task CancelBillingChargeAsync(
        string chargeId,
        string actorId,
        string reason,
        CancellationToken cancellationToken = default)
        => Task.FromException(new InvalidOperationException("ITrkBillingChargePort adapter is not configured."));

    public void CancelBillingCharge(string chargeId, string actorId, string reason)
        => throw new InvalidOperationException("ITrkBillingChargePort adapter is not configured.");
}
