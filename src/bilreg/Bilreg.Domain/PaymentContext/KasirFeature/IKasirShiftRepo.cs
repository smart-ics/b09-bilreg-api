using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface IKasirShiftRepo :
    ISaveChange<KasirShiftModel>,
    ILoadEntity<KasirShiftModel, IKasirShiftKey>
{
    void Save(KasirShiftModel model);
    KasirShiftModel? GetKey(IKasirShiftKey key);
    KasirShiftModel? GetKey(string shiftId);
    KasirShiftModel? GetActiveShift(string kasirId);
    IEnumerable<KasirShiftModel> ListByTgl(string tglShift);
}
