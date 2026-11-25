namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface ITrsKasirItemDal
{
    void Insert(TrsKasirItemDto item);
    void Insert(IEnumerable<TrsKasirItemDto> items);
    IEnumerable<TrsKasirItemDto> ListDataByTrsKasir(string trsKasirId);
    IEnumerable<string> ListPaidBillIdsByReg(string regId);
    IEnumerable<BillingUnpaidItemDto> ListUnpaidBillingByReg(string regId);
}
