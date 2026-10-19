using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class BillingLockChecker : IBillingLockChecker
{
    private readonly IBilrgTataRekeningDal _tataRekeningDal;

    public BillingLockChecker(IBilrgTataRekeningDal tataRekeningDal)
    {
        _tataRekeningDal = tataRekeningDal;
    }

    public void AssertBillingNotFinal(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return;

        var dto = _tataRekeningDal.GetData(new RegKey(regId));
        if (dto != null && (dto.Status == (int)TataRekeningStatusEnum.Finalized || dto.Status == (int)TataRekeningStatusEnum.Lunas))
        {
            throw new BillingEpisodeFinalException(regId);
        }
    }

    private record RegKey(string RegId) : IRegKey;
}
