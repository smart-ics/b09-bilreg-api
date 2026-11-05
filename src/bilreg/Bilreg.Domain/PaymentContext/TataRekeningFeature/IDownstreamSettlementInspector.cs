namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Port untuk memeriksa transaksi hilir (alokasi pembayaran disetujui dan settlement kasir)
/// pada satu registrasi sebelum pembatalan finalisasi diizinkan.
/// </summary>
public interface IDownstreamSettlementInspector
{
    /// <summary>
    /// True bila alokasi pembayaran hilir telah disetujui, misal alokasi ke penjamin.
    /// </summary>
    bool HasApprovedAllocation(string regId);

    /// <summary>
    /// True bila transaksi kasir telah diproses untuk registrasi tersebut.
    /// </summary>
    bool HasCashierSettlement(string regId);
}
