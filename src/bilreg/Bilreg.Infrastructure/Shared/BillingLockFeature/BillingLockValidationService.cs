using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.Shared.BillingLockFeature;

public class BillingLockValidationService : IBillingLockValidationService
{
    private readonly ITataRekeningRepo _tataRekeningRepo;

    public BillingLockValidationService(ITataRekeningRepo tataRekeningRepo)
    {
        _tataRekeningRepo = tataRekeningRepo;
    }

    public Task EnsureCanModifyBillingAsync(string regId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return Task.CompletedTask;

        cancellationToken.ThrowIfCancellationRequested();

        var maybeTataRekening = _tataRekeningRepo.LoadEntity(RegModel.Key(regId));
        if (maybeTataRekening.HasValue)
        {
            var tataRekening = maybeTataRekening.Value;
            tataRekening.EnsureCanModifyBill();
        }

        return Task.CompletedTask;
    }
}
