using Bilreg.Domain.PaymentContext.KasirFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public interface ITrsKasirDal :
    IInsert<TrsKasirDto>,
    IUpdate<TrsKasirDto>,
    IGetData<TrsKasirDto, ITrsKasirKey>
{
    void InsertPayments(IEnumerable<TrsKasirPaymentDto> payments);
    void DeletePayments(ITrsKasirKey key);
    IEnumerable<TrsKasirPaymentDto> ListPayments(ITrsKasirKey key);
    IEnumerable<TrsKasirDto> ListByShift(string shiftId);
    IEnumerable<TrsKasirDto> ListByReg(string regId);
    IEnumerable<TrsKasirDto> ListByTgl(string tglTrs);
}
