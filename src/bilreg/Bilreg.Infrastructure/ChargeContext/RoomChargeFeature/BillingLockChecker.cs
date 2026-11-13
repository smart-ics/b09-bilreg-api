using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.Shared.BillingLockFeature;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class BillingLockChecker : IBillingLockChecker
{
    private readonly IBillingLockValidationService _billingLockValidator;

    public BillingLockChecker(IBillingLockValidationService billingLockValidator)
    {
        _billingLockValidator = billingLockValidator;
    }

    public void AssertBillingNotFinal(string regId)
    {
        _billingLockValidator.EnsureCanModifyBillingAsync(regId).GetAwaiter().GetResult();
    }
}
