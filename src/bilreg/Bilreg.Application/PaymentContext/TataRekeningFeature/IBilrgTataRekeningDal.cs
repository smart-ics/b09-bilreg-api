namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public interface IBilrgTataRekeningDal
{
    IEnumerable<TataRekeningQueueDto> ListDataQueue(int? jenisReg = null);
}
