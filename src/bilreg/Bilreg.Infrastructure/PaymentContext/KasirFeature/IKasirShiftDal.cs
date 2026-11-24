using Bilreg.Domain.PaymentContext.KasirFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public interface IKasirShiftDal :
    IInsert<KasirShiftDto>,
    IUpdate<KasirShiftDto>,
    IGetData<KasirShiftDto, IKasirShiftKey>
{
    KasirShiftDto? GetActiveShift(string kasirId);
    IEnumerable<KasirShiftDto> ListByTgl(string tglShift);
}
