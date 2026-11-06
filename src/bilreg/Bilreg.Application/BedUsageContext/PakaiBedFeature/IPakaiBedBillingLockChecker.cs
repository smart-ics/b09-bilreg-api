using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public class BillingLockedException : BillingLockViolationException
{
    public BillingLockedException(string regId)
        : base(regId, TataRekeningStatusEnum.Finalized, $"Episode billing untuk registrasi '{regId}' telah berstatus Final / Lunas dan terkunci dari perubahan.")
    {
    }

    public BillingLockedException(string regId, string message)
        : base(regId, message)
    {
    }
}

public interface IPakaiBedBillingLockChecker
{
    bool CheckIsBillingLocked(string regId);
    void AssertBillingNotLocked(string regId);
}
