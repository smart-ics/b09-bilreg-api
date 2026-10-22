using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public class BillingEpisodeFinalException : BillingLockViolationException
{
    public BillingEpisodeFinalException(string regId)
        : base(regId, TataRekeningStatusEnum.Finalized, $"Episode penagihan untuk registrasi '{regId}' telah berstatus Final / Lunas dan terkunci dari perubahan.")
    {
    }

    public BillingEpisodeFinalException(string regId, string message)
        : base(regId, message)
    {
    }
}

public interface IBillingLockChecker
{
    void AssertBillingNotFinal(string regId);
}
