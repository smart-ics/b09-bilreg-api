namespace Bilreg.Application.Shared.BillingLockFeature;

public interface IBillingLockValidationService
{
    Task EnsureCanModifyBillingAsync(string regId, CancellationToken cancellationToken = default);
}
