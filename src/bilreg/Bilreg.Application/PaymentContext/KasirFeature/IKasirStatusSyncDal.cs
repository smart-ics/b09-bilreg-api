namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface IKasirStatusSyncDal
{
    void SyncTerima(string orderRef, string kasirId, string tglTrs, string jamTrs, string trsKasirId, decimal totalNilai, string regId = "");
    void SyncKeluar(string orderRef, string kasirId, string tglTrs, string jamTrs, string trsKasirId, decimal totalNilai, string regId = "");
    void Reversal(string orderRef, string trsKasirId, string regId = "");
}
