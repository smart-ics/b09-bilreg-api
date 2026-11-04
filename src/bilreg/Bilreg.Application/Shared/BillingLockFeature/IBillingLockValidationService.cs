namespace Bilreg.Application.Shared.BillingLockFeature;

public interface IBillingLockValidationService
{
    Task EnsureCanModifyBillingAsync(string regId, CancellationToken cancellationToken = default);
}

public class NullBillingLockValidationService : IBillingLockValidationService
{
    public Task EnsureCanModifyBillingAsync(string regId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
