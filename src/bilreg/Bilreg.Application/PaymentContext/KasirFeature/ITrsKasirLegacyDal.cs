namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface ITrsKasirLegacyDal
{
    void Insert(TrsKasirLegacyHdrDto hdr, IEnumerable<TrsKasirLegacyDtlDto> details);
    void Void(string trsKasirId, string tglVoid, string jamVoid, string petugasVoid);
    TrsKasirLegacyHdrDto? GetHeader(string trsKasirId);
    IEnumerable<TrsKasirLegacyDtlDto> ListDetails(string trsKasirId);
    void Delete(string trsKasirId);
}
