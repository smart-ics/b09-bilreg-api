using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public class PakaiBedBillingLockChecker : IPakaiBedBillingLockChecker
{
    private readonly IBillingLockValidationService? _validationService;
    private readonly IBillingLockChecker? _billingLockChecker;

    public PakaiBedBillingLockChecker(
        IBillingLockValidationService? validationService = null,
        IBillingLockChecker? billingLockChecker = null)
    {
        _validationService = validationService;
        _billingLockChecker = billingLockChecker;
    }

    public bool CheckIsBillingLocked(string regId)
    {
        try
        {
            AssertBillingNotLocked(regId);
            return false;
        }
        catch (BillingLockViolationException)
        {
            return true;
        }
    }

    public void AssertBillingNotLocked(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId)) return;

        try
        {
            if (_billingLockChecker != null)
            {
                _billingLockChecker.AssertBillingNotFinal(regId);
            }
            else if (_validationService != null)
            {
                _validationService.EnsureCanModifyBillingAsync(regId).GetAwaiter().GetResult();
            }
        }
        catch (BillingLockViolationException ex)
        {
            throw new BillingLockedException(regId, ex.Message);
        }
    }
}
