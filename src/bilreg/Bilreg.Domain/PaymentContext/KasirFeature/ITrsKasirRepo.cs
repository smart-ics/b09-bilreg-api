using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface ITrsKasirRepo :
    ISaveChange<TrsKasirModel>,
    ILoadEntity<TrsKasirModel, ITrsKasirKey>
{
    void Save(TrsKasirModel model);
    TrsKasirModel? GetKey(ITrsKasirKey key);
    TrsKasirModel? GetKey(string trsKasirId);
    IEnumerable<TrsKasirModel> ListByShift(IKasirShiftKey shiftKey);
    IEnumerable<TrsKasirModel> ListByShift(string shiftId);
    IEnumerable<TrsKasirModel> ListByReg(string regId);
    IEnumerable<TrsKasirModel> ListByTgl(string tglTrs);
}
